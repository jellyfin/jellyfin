using MediaBrowser.Common.Kernel;
using MediaBrowser.Model.Entities;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net.Handlers
{
    /// <summary>
    /// Class BaseActionHandler
    /// </summary>
    /// <typeparam name="TKernelType">The type of the T kernel type.</typeparam>
    public abstract class BaseActionHandler<TKernelType> : BaseSerializationHandler<TKernelType, EmptyRequestResult>
        where TKernelType : IKernel
    {
        /// <summary>
        /// Gets the object to serialize.
        /// </summary>
        /// <returns>Task{EmptyRequestResult}.</returns>
        protected override async Task<EmptyRequestResult> GetObjectToSerialize()
        {
            await ExecuteAction();

            return new EmptyRequestResult();
        }

        /// <summary>
        /// Performs the action.
        /// </summary>
        /// <returns>Task.</returns>
        protected abstract Task ExecuteAction();
    }
}
