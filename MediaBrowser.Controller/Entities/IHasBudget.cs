
namespace MediaBrowser.Controller.Entities
{
    public interface IHasBudget
    {
        /// <summary>
        /// Gets or sets the budget.
        /// </summary>
        /// <value>The budget.</value>
        double? Budget { get; set; }

        /// <summary>
        /// Gets or sets the revenue.
        /// </summary>
        /// <value>The revenue.</value>
        double? Revenue { get; set; }
    }
}
