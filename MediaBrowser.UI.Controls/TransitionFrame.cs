using Microsoft.Expression.Media.Effects;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// Class TransitionFrame
    /// </summary>
    public class TransitionFrame : Frame
    {
        /// <summary>
        /// The _content presenter
        /// </summary>
        private ContentPresenter _contentPresenter = null;

        #region DP TransitionType

        /// <summary>
        /// Gets or sets the type of the transition.
        /// </summary>
        /// <value>The type of the transition.</value>
        public TransitionEffect TransitionType
        {
            get { return (TransitionEffect)GetValue(TransitionTypeProperty); }
            set { SetValue(TransitionTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TransitionType.  This enables animation, styling, binding, etc...
        /// <summary>
        /// The transition type property
        /// </summary>
        public static readonly DependencyProperty TransitionTypeProperty =
            DependencyProperty.Register("TransitionType", typeof(TransitionEffect), typeof(TransitionFrame),
            new UIPropertyMetadata(new BlindsTransitionEffect()));

        #endregion DP TransitionType

        #region DP Transition Animation

        /// <summary>
        /// Gets or sets the transition animation.
        /// </summary>
        /// <value>The transition animation.</value>
        public DoubleAnimation TransitionAnimation
        {
            get { return (DoubleAnimation)GetValue(TransitionAnimationProperty); }
            set { SetValue(TransitionAnimationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TransitionAnimation.  This enables animation, styling, binding, etc...
        /// <summary>
        /// The transition animation property
        /// </summary>
        public static readonly DependencyProperty TransitionAnimationProperty =
            DependencyProperty.Register("TransitionAnimation", typeof(DoubleAnimation), typeof(TransitionFrame), new UIPropertyMetadata(null));

        #endregion DP Transition Animation

        /// <summary>
        /// Called when the template generation for the visual tree is created.
        /// </summary>
        public override void OnApplyTemplate()
        {
            // get a reference to the frame's content presenter
            // this is the element we will fade in and out
            _contentPresenter = GetTemplateChild("PART_FrameCP") as ContentPresenter;
            base.OnApplyTemplate();
        }

        /// <summary>
        /// Animates the content.
        /// </summary>
        /// <param name="navigationAction">The navigation action.</param>
        /// <param name="checkContent">if set to <c>true</c> [check content].</param>
        /// <param name="isBack">if set to <c>true</c> [is back].</param>
        private void AnimateContent(Action navigationAction, bool checkContent = true, bool isBack = false)
        {
            if (TransitionType == null || (checkContent && Content == null))
            {
                CommandBindings.Clear();
                navigationAction();
                CommandBindings.Clear();
                return;
            }
            
            var oldContentVisual = this as FrameworkElement;

            _contentPresenter.IsHitTestVisible = false;

            var da = TransitionAnimation.Clone();
            da.From = 0;
            da.To = 1;
            da.FillBehavior = FillBehavior.HoldEnd;

            var transitionEffect = TransitionType.Clone() as TransitionEffect;

            if (isBack)
            {
                ReverseDirection(transitionEffect);
            }

            transitionEffect.OldImage = new VisualBrush(oldContentVisual);
            transitionEffect.BeginAnimation(TransitionEffect.ProgressProperty, da);

            _contentPresenter.Effect = transitionEffect;
            _contentPresenter.IsHitTestVisible = true;

            // Remove base class bindings to remote buttons
            CommandBindings.Clear();

            navigationAction();

            CommandBindings.Clear();
        }

        /// <summary>
        /// Navigates the with transition.
        /// </summary>
        /// <param name="page">The page.</param>
        public void NavigateWithTransition(Page page)
        {
            AnimateContent(() => Navigate(page));
        }

        /// <summary>
        /// Navigates the with transition.
        /// </summary>
        /// <param name="page">The page.</param>
        public void NavigateWithTransition(Uri page)
        {
            AnimateContent(() => Navigate(page));
        }

        /// <summary>
        /// Goes the back with transition.
        /// </summary>
        public void GoBackWithTransition()
        {
            if (CanGoBack)
            {
                AnimateContent(GoBack, false, true);
            }
        }

        /// <summary>
        /// Goes the forward with transition.
        /// </summary>
        public void GoForwardWithTransition()
        {
            if (CanGoForward)
            {
                AnimateContent(GoForward, false);
            }
        }

        /// <summary>
        /// Reverses the direction.
        /// </summary>
        /// <param name="transitionEffect">The transition effect.</param>
        private void ReverseDirection(TransitionEffect transitionEffect)
        {
            var circleRevealTransitionEffect = transitionEffect as CircleRevealTransitionEffect;

            if (circleRevealTransitionEffect != null)
            {
                circleRevealTransitionEffect.Reverse = true;
                return;
            }

            var slideInTransitionEffect = transitionEffect as SlideInTransitionEffect;
            if (slideInTransitionEffect != null)
            {
                if (slideInTransitionEffect.SlideDirection == SlideDirection.RightToLeft)
                {
                    slideInTransitionEffect.SlideDirection = SlideDirection.LeftToRight;
                }
                return;
            }

            var wipeTransitionEffect = transitionEffect as WipeTransitionEffect;
            if (wipeTransitionEffect != null)
            {
                if (wipeTransitionEffect.WipeDirection == WipeDirection.RightToLeft)
                {
                    wipeTransitionEffect.WipeDirection = WipeDirection.LeftToRight;
                }
            }
        }
    }
}
