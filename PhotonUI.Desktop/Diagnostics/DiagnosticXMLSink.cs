using PhotonUI.Controls;
using PhotonUI.Diagnostics;
using PhotonUI.Diagnostics.Events;
using PhotonUI.Diagnostics.Events.Framework;
using PhotonUI.Diagnostics.Events.Platform;
using PhotonUI.Models;
using SDL3;
using Serilog;
using System.Reflection;
using System.Text;

namespace PhotonUI.Desktop.Diagnostics
{
    public class DiagnosticXMLSink : IPhotonDiagnostics
    {
        protected ILogger? Logger = null;

        protected readonly Stack<bool> WriteStack = new();

        protected bool ShouldIncludeParameters = false;
        protected bool ShouldIncludeParameterNames = false;
        protected bool ShouldIncludeTimestamps = false;

        protected bool Running = false;
        protected string Path = "photonui.debug.xml";
        protected string Header = "";
        protected string Footer = "";

        private readonly Stack<DiagnosticEventArgs> pendingGroupStarts = [];
        private readonly Stack<bool> groupWriteStack = new();

        public Func<DiagnosticEventArgs, bool>? GroupPredicate { get; set; }
        public List<Func<DiagnosticEventArgs, bool>> ScopeFilters { get; } = [];

        public DiagnosticXMLSink()
        {
            this.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(this.Path, shared: true, outputTemplate: "{Message:l}{NewLine}")
                .CreateLogger();

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                this.Stop();
            };
        }

        #region DiagnosticXMLSink: Fluent API

        public DiagnosticXMLSink ResetOutput()
        {
            (this.Logger as IDisposable)?.Dispose();

            if (File.Exists(this.Path))
                File.Delete(this.Path);

            this.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(this.Path, shared: true, outputTemplate: "{Message:l}{NewLine}")
                .CreateLogger();

            return this;
        }
        public DiagnosticXMLSink SetOutput(string path)
        {
            (this.Logger as IDisposable)?.Dispose();

            this.Path = path;

            this.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(this.Path, shared: true, outputTemplate: "{Message:l}{NewLine}")
                .CreateLogger();
            return this;
        }

        public DiagnosticXMLSink SetHeader(string header)
        {
            this.Header = header;
            return this;
        }
        public DiagnosticXMLSink SetFooter(string footer)
        {
            this.Footer = footer;
            return this;
        }

        public DiagnosticXMLSink AddScopeFilter(Func<DiagnosticEventArgs, bool> filter)
        {
            this.ScopeFilters.Add(filter);
            return this;
        }
        public DiagnosticXMLSink SetGrouping(Func<DiagnosticEventArgs, bool> predicate)
        {
            this.GroupPredicate = predicate;
            return this;
        }

        public DiagnosticXMLSink IncludeParameters(bool showArgs = true, bool showArgNames = true)
        {
            this.ShouldIncludeParameters = showArgs;
            this.ShouldIncludeParameterNames = showArgNames;
            return this;
        }
        public DiagnosticXMLSink IncludeTimestamps(bool show = true)
        {
            this.ShouldIncludeTimestamps = show;
            return this;
        }

        #endregion

        public void Start()
        {
            if (this.Running) return;

            this.Running = true;
            this.Logger?.Information(this.Header);
        }
        public void Stop()
        {
            if (!this.Running) return;

            this.Running = false;
            this.Logger?.Information(this.Footer);

            (this.Logger as IDisposable)?.Dispose();
        }

        public void OnEvent(DiagnosticEventArgs e)
        {
            if (!this.Running) return;

            bool isStart = e.Phase == DiagnosticPhase.Start;
            bool isEnd = e.Phase == DiagnosticPhase.End;

            bool hasScopeFilters = this.ScopeFilters.Count > 0;
            bool writeIsOpen = this.WriteStack.Count > 0 && this.WriteStack.Peek();

            bool matchGroupPredicate = this.GroupPredicate != null && this.GroupPredicate(e);
            bool matchScopePredicate = this.ScopeFilters.Any(f => f(e));

            bool shouldWrite = !hasScopeFilters || writeIsOpen || matchScopePredicate;

            if (matchGroupPredicate)
            {
                if (isStart)
                {
                    this.pendingGroupStarts.Push(e);
                    this.groupWriteStack.Push(false);
                }
                else if (isEnd && this.groupWriteStack.Count > 0)
                {
                    bool wasWritten = this.groupWriteStack.Pop();
                    DiagnosticEventArgs startEvent = this.pendingGroupStarts.Pop();

                    if (wasWritten)
                    {
                        this.WriteEventXml(e);
                    }
                }

                return;
            }

            if (this.pendingGroupStarts.Count > 0 && matchScopePredicate)
            {
                DiagnosticEventArgs[] pending = [.. this.pendingGroupStarts];
                var writeFlags = this.groupWriteStack.ToArray();

                this.pendingGroupStarts.Clear();
                this.groupWriteStack.Clear();

                for (int i = pending.Length - 1; i >= 0; i--)
                {
                    DiagnosticEventArgs evt = pending[i];
                    bool alreadyWritten = writeFlags[i];

                    if (!alreadyWritten)
                    {
                        this.WriteEventXml(evt);

                        alreadyWritten = true;
                    }

                    this.pendingGroupStarts.Push(evt);
                    this.groupWriteStack.Push(alreadyWritten);
                }
            }

            if (isStart && matchScopePredicate)
                this.WriteStack.Push(true);

            if (shouldWrite)
                this.WriteEventXml(e);

            if (isEnd && matchScopePredicate)
                if (this.WriteStack.Count > 0)
                    this.WriteStack.Pop();
        }

        #region DiagnosticXMLSink: XML Writers

        protected void WriteEventXml(DiagnosticEventArgs e)
        {
            string attr = this.FormatAttributes(e);

            switch (e)
            {
                case PlatformEventEventArgs @event:
                    this.WritePlatformXml(@event, attr);
                    break;

                case PhotonMethodEventArgs method:
                    this.WritePhotonXml(method, attr, this.FormatParameters(e.MethodInfo, method.Parameters));
                    break;

                case ControlMethodEventArgs method:
                    this.WriteControlMethodXml(method, attr, this.FormatParameters(e.MethodInfo, method.Parameters));
                    break;

                case ControlEventArgs control:
                    this.WriteControlXml(control, attr);
                    break;
            }
        }
        protected void WritePlatformXml(PlatformEventEventArgs platform, string attr)
        {
            SDL.EventType eventType = (SDL.EventType)platform.Event.NativeEvent.Type;

            // Build extra attributes
            string extra = $"Preview=\"{platform.Event.Preview}\" Handled=\"{platform.Event.Handled}\"";

            switch (platform.Phase)
            {
                case DiagnosticPhase.Atomic:
                case DiagnosticPhase.Start:
                    this.Logger?.Information(
                        "<{0} {1}{2} {3}",
                        eventType,
                        extra,
                        string.IsNullOrWhiteSpace(attr) ? "" : " " + attr.Trim(),
                        platform.Phase == DiagnosticPhase.Atomic ? "/>" : ">");
                    break;

                case DiagnosticPhase.End:
                    this.Logger?.Information("</{0}>", eventType);
                    break;
            }
        }

        protected void WritePhotonXml(PhotonMethodEventArgs method, string attr, string args)
        {
            switch (method.Phase)
            {
                case DiagnosticPhase.Atomic:
                case DiagnosticPhase.Start:
                    string closing = method.Phase == DiagnosticPhase.Atomic ? "/>" : ">";
                    if (string.IsNullOrEmpty(args))
                        this.Logger?.Information("<{0} {1} {2}", method.MethodInfo?.Name, attr, closing);
                    else
                        this.Logger?.Information("<{0} Params=\"{1}\" {2} {3}", method.MethodInfo?.Name, args, attr, closing);
                    break;

                case DiagnosticPhase.End:
                    this.Logger?.Information("</{0}>", method.MethodInfo?.Name);
                    break;
            }
        }
        protected void WriteControlMethodXml(ControlEventArgs method, string attr, string args)
        {
            string methodName = method.MethodInfo != null
                ? $"{method.MethodInfo.DeclaringType?.Name}.{method.MethodInfo.Name}"
                : method.MethodInfo?.Name ?? "UnknownMethod";

            switch (method.Phase)
            {
                case DiagnosticPhase.Atomic:
                case DiagnosticPhase.Start:
                    string closing = method.Phase == DiagnosticPhase.Atomic ? "/>" : ">";
                    if (string.IsNullOrEmpty(args))
                        this.Logger?.Information(
                            "<{0} Control=\"{1}\" ControlType=\"{2}\" {3} {4}",
                            methodName,
                            method.Control.Name,
                            method.Control.GetType().Name,
                            attr,
                            closing);
                    else
                        this.Logger?.Information(
                            "<{0} Params=\"{1}\" Control=\"{2}\" ControlType=\"{3}\" {4} {5}",
                            methodName,
                            args,
                            method.Control.Name,
                            method.Control.GetType().Name,
                            attr,
                            closing);
                    break;

                case DiagnosticPhase.End:
                    this.Logger?.Information("</{0}>", methodName);
                    break;
            }
        }
        protected void WriteControlXml(ControlEventArgs method, string attr)
        {
            switch (method.Phase)
            {
                case DiagnosticPhase.Atomic:
                case DiagnosticPhase.Start:
                    this.Logger?.Information(
                        "<{0} Control=\"{1}\" ControlType=\"{2}\" {3} {4}",
                        method.GetType().Name,
                        method.Control.Name,
                        method.Control.GetType().Name,
                        attr,
                        method.Phase == DiagnosticPhase.Atomic ? "/>" : ">");
                    break;

                case DiagnosticPhase.End:
                    this.Logger?.Information("</{0}>", method.GetType().Name);
                    break;
            }
        }

        #endregion

        #region DiagnosticXMLSink: Helpers

        protected string FormatAttributes(DiagnosticEventArgs e)
        {
            StringBuilder attributes = new();

            if (this.ShouldIncludeTimestamps)
            {
                if (SDL3.SDL.GetCurrentTime(out long tick))
                    attributes.Append($"Tick=\"{tick}\" ");
            }

            return attributes.ToString();
        }
        protected string FormatParameters(MethodInfo? method, List<object?> values)
        {
            if (method == null)
                return string.Empty;

            if (!this.ShouldIncludeParameters)
                return string.Empty;

            ParameterInfo[] parameters = method.GetParameters();
            List<string> parts = [];

            for (int i = 0; i < parameters.Length && i < values.Count; i++)
            {
                ParameterInfo p = parameters[i];
                var value = values[i];

                if (p.ParameterType == typeof(Window) || typeof(Delegate).IsAssignableFrom(p.ParameterType))
                    continue;

                string formatted = value switch
                {
                    Size s => $"({s.Width}x{s.Height})",
                    SDL3.SDL.FPoint pnt => $"({pnt.X},{pnt.Y})",
                    SDL3.SDL.Rect r => $"({r.X},{r.Y},{r.W},{r.H})",
                    _ => value?.ToString() ?? "null"
                };

                if (this.ShouldIncludeParameterNames)
                    parts.Add($"{p.Name}={formatted}");
                else
                    parts.Add(formatted);
            }

            return string.Join(", ", parts);
        }

        #endregion
    }
}