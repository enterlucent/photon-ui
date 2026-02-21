using PhotonUI.Controls;
using System.ComponentModel;

namespace PhotonUI.Components
{
    public class PropertyBinder
    {
        public readonly Control Target;
        public readonly string TargetProperty;
        public readonly object Source;
        public readonly string SourceProperty;
        public readonly bool TwoWay;

        private readonly Func<object, object?> sourceGetter;
        private readonly Action<object, object?>? sourceSetter;
        private readonly Func<object, object?> targetGetter;
        private readonly Action<object, object?>? targetSetter;

        public PropertyBinder(
            Control target, string targetProperty, object source, string sourceProperty,
            Func<object, object?> sourceGetter, Action<object, object?>? sourceSetter,
            Func<object, object?> targetGetter, Action<object, object?>? targetSetter,
            bool twoWay = false)
        {
            this.Target = target;
            this.TargetProperty = targetProperty;
            this.Source = source;
            this.SourceProperty = sourceProperty;
            this.TwoWay = twoWay;

            this.sourceGetter = sourceGetter;
            this.sourceSetter = sourceSetter;
            this.targetGetter = targetGetter;
            this.targetSetter = targetSetter;

            this.UpdateTarget();

            if (this.Source is INotifyPropertyChanged npcSource)
            {
                npcSource.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == this.SourceProperty)
                        this.UpdateTarget();
                };
            }

            if (this.TwoWay && this.Target is INotifyPropertyChanged npcTarget)
            {
                npcTarget.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == this.TargetProperty)
                        this.UpdateSource();
                };
            }
        }

        private void UpdateTarget()
        {
            object? value = this.sourceGetter(this.Source);

            this.targetSetter?.Invoke(this.Target, value);
        }

        private void UpdateSource()
        {
            object? value = this.targetGetter(this.Target);

            this.sourceSetter?.Invoke(this.Source, value);
        }
    }
}