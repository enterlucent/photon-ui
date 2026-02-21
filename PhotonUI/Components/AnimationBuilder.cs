using PhotonUI.Animations;
using PhotonUI.Controls;
using PhotonUI.Interfaces.Services;
using System.Linq.Expressions;
using System.Reflection;

namespace PhotonUI.Components
{
    public class AnimationBuilder(IInterpolatorService lerpService, IBindingService bindingService) : IAnimationBuilder
    {
        private readonly IInterpolatorService lerpService = lerpService;
        private readonly IBindingService bindingService = bindingService;

        public PropertyAnimation<TTarget, TProp> BuildPropertyAnimation<TTarget, TProp>(TTarget target, Expression<Func<TTarget, TProp>> selector)
            where TTarget : Control
        {
            if (selector.Body is not MemberExpression member || member.Member is not PropertyInfo propInfo)
                throw new InvalidOperationException("Selector must be a property expression");

            PropertyAnimation<TTarget, TProp> anim = new(this.lerpService);

            this.bindingService.Bind(target, propInfo.Name, anim, nameof(anim.Value), twoWay: false);

            return anim;
        }
    }
}