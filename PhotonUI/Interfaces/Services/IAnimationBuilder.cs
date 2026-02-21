using PhotonUI.Animations;
using PhotonUI.Controls;
using System.Linq.Expressions;

namespace PhotonUI.Interfaces.Services
{
    public interface IAnimationBuilder
    {
        PropertyAnimation<TTarget, TProp> BuildPropertyAnimation<TTarget, TProp>(TTarget target, Expression<Func<TTarget, TProp>> selector) where TTarget : Control;
    }
}