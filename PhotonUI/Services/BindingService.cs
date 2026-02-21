using PhotonUI.Components;
using PhotonUI.Controls;
using PhotonUI.Interfaces.Services;
using System.Linq.Expressions;
using System.Reflection;

namespace PhotonUI.Services
{
    public class BindingService : IBindingService
    {
        private readonly List<PropertyBinder> bindings = [];

        private static Func<object, object?> BuildGetter(PropertyInfo prop)
        {
            ParameterExpression objParam = Expression.Parameter(typeof(object), "obj");
            UnaryExpression castObj = Expression.Convert(objParam, prop.DeclaringType!);
            MemberExpression propAccess = Expression.Property(castObj, prop);
            UnaryExpression castResult = Expression.Convert(propAccess, typeof(object));

            return Expression.Lambda<Func<object, object?>>(castResult, objParam).Compile();
        }
        private static Action<object, object?>? BuildSetter(PropertyInfo prop)
        {
            if (!prop.CanWrite)
                return null;

            ParameterExpression objParam = Expression.Parameter(typeof(object), "obj");
            ParameterExpression valParam = Expression.Parameter(typeof(object), "val");

            UnaryExpression castObj = Expression.Convert(objParam, prop.DeclaringType!);
            UnaryExpression castVal = Expression.Convert(valParam, prop.PropertyType);

            MethodCallExpression call = Expression.Call(castObj, prop.SetMethod!, castVal);

            return Expression.Lambda<Action<object, object?>>(call, objParam, valParam).Compile();
        }

        public void Bind(Control target, string targetProperty, object source, string sourceProperty, bool twoWay = false)
        {
            PropertyInfo? sourceProp = source.GetType().GetProperty(sourceProperty, BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo? targetProp = target.GetType().GetProperty(targetProperty, BindingFlags.Public | BindingFlags.Instance);

            if (sourceProp == null || targetProp == null)
                throw new InvalidOperationException($"Binding failed: property not found (source={sourceProperty}, target={targetProperty})");

            Func<object, object?> sourceGetter = BuildGetter(sourceProp);
            Action<object, object?>? sourceSetter = BuildSetter(sourceProp);
            Func<object, object?> targetGetter = BuildGetter(targetProp);
            Action<object, object?>? targetSetter = BuildSetter(targetProp);

            PropertyBinder binding = new(target, targetProperty, source, sourceProperty, sourceGetter, sourceSetter, targetGetter, targetSetter, twoWay);

            this.bindings.Add(binding);
        }
        public void Unbind(Control target, string targetProperty)
            => this.bindings.RemoveAll(b => b.Target == target && b.TargetProperty == targetProperty);
    }
}