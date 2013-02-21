using Microsoft.Expression.Media.Effects;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// http://victorcher.blogspot.com/2012/02/wpf-transactions.html
    /// </summary>
    public class TransitionControl : ContentControl
    {
        /// <summary>
        /// The _content presenter
        /// </summary>
        private ContentPresenter _contentPresenter;

        /// <summary>
        /// Initializes static members of the <see cref="TransitionControl" /> class.
        /// </summary>
        static TransitionControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TransitionControl), new FrameworkPropertyMetadata(typeof(TransitionControl)));

            ContentProperty.OverrideMetadata(
                typeof(TransitionControl), new FrameworkPropertyMetadata(OnContentPropertyChanged));
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _contentPresenter = (ContentPresenter)Template.FindName("ContentPresenter", this);
        }

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
            DependencyProperty.Register("TransitionType", typeof(TransitionEffect), typeof(TransitionControl),
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
            DependencyProperty.Register("TransitionAnimation", typeof(DoubleAnimation), typeof(TransitionControl), new UIPropertyMetadata(null));

        #endregion DP Transition Animation

        /// <summary>
        /// Called when [content property changed].
        /// </summary>
        /// <param name="dp">The dp.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void OnContentPropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs args)
        {
            var oldContent = args.OldValue;
            var newContent = args.NewValue;

            var transitionControl = (TransitionControl)dp;

            if (DesignerProperties.GetIsInDesignMode(transitionControl))
                return;

            if (oldContent != null && newContent != null && transitionControl.IsVisible)
            {
                transitionControl.AnimateContent(oldContent, newContent);
            }
            else if (newContent != null)
            {
                transitionControl.Content = newContent;
            }
        }

        /// <summary>
        /// Animates the content.
        /// </summary>
        /// <param name="oldContent">The old content.</param>
        /// <param name="newContent">The new content.</param>
        private void AnimateContent(object oldContent, object newContent)
        {
            FrameworkElement oldContentVisual;

            try
            {
                oldContentVisual = VisualTreeHelper.GetChild(_contentPresenter, 0) as FrameworkElement;
            }
            catch
            {
                return;
            }

            var transitionEffect = TransitionType;

            if (transitionEffect == null)
            {
                _contentPresenter.Content = newContent;
                return;
            }

            var da = TransitionAnimation;
            da.From = 0;
            da.To = 1;
            da.FillBehavior = FillBehavior.HoldEnd;

            transitionEffect.OldImage = new VisualBrush(oldContentVisual);
            transitionEffect.BeginAnimation(TransitionEffect.ProgressProperty, da);

            _contentPresenter.Effect = transitionEffect;
            _contentPresenter.Content = newContent;
        }
    }
}