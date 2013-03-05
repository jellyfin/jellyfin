using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Implementations.HttpServer;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.Text.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetUsers
    /// </summary>
    [Route("/Users", "GET")]
    public class GetUsers : IReturn<List<UserDto>>
    {
    }

    /// <summary>
    /// Class GetUser
    /// </summary>
    [Route("/Users/{Id}", "GET")]
    public class GetUser : IReturn<UserDto>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class DeleteUser
    /// </summary>
    [Route("/Users/{Id}", "DELETE")]
    public class DeleteUser : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class AuthenticateUser
    /// </summary>
    [Route("/Users/{Id}/Authenticate", "POST")]
    public class AuthenticateUser : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        [ApiMember(Name = "Password", IsRequired = true, DataType = "string", ParameterType = "body", Verb = "GET")]
        public string Password { get; set; }
    }

    /// <summary>
    /// Class UpdateUserPassword
    /// </summary>
    [Route("/Users/{Id}/Password", "POST")]
    public class UpdateUserPassword : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        public string CurrentPassword { get; set; }

        /// <summary>
        /// Gets or sets the new password.
        /// </summary>
        /// <value>The new password.</value>
        public string NewPassword { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [reset password].
        /// </summary>
        /// <value><c>true</c> if [reset password]; otherwise, <c>false</c>.</value>
        public bool ResetPassword { get; set; }
    }

    /// <summary>
    /// Class UpdateUser
    /// </summary>
    [Route("/Users/{Id}", "POST")]
    public class UpdateUser : UserDto, IReturnVoid
    {
    }

    /// <summary>
    /// Class CreateUser
    /// </summary>
    [Route("/Users", "POST")]
    public class CreateUser : UserDto, IReturn<UserDto>
    {
    }

    /// <summary>
    /// Class UsersService
    /// </summary>
    public class UserService : BaseRestService
    {
        /// <summary>
        /// The _XML serializer
        /// </summary>
        private readonly IXmlSerializer _xmlSerializer;

        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UserService" /> class.
        /// </summary>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <exception cref="System.ArgumentNullException">xmlSerializer</exception>
        public UserService(IXmlSerializer xmlSerializer, IJsonSerializer jsonSerializer, IUserManager userManager)
            : base()
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }

            if (xmlSerializer == null)
            {
                throw new ArgumentNullException("xmlSerializer");
            }

            _jsonSerializer = jsonSerializer;
            _xmlSerializer = xmlSerializer;
            _userManager = userManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetUsers request)
        {
            var dtoBuilder = new DtoBuilder(Logger);

            var result = _userManager.Users.OrderBy(u => u.Name).Select(dtoBuilder.GetDtoUser).ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetUser request)
        {
            var user = _userManager.GetUserById(request.Id);

            if (user == null)
            {
                throw new ResourceNotFoundException("User not found");
            }

            var result = new DtoBuilder(Logger).GetDtoUser(user);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteUser request)
        {
            var user = _userManager.GetUserById(request.Id);

            if (user == null)
            {
                throw new ResourceNotFoundException("User not found");
            }

            var task = _userManager.DeleteUser(user);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(AuthenticateUser request)
        {
            var user = _userManager.GetUserById(request.Id);

            if (user == null)
            {
                throw new ResourceNotFoundException("User not found");
            }

            var success = _userManager.AuthenticateUser(user, request.Password).Result;

            if (!success)
            {
                // Unauthorized
                throw new ResourceNotFoundException("Invalid user or password entered.");
            }
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateUserPassword request)
        {
            var user = _userManager.GetUserById(request.Id);

            if (user == null)
            {
                throw new ResourceNotFoundException("User not found");
            }

            if (request.ResetPassword)
            {
                var task = user.ResetPassword(_userManager);

                Task.WaitAll(task);
            }
            else
            {
                var success = _userManager.AuthenticateUser(user, request.CurrentPassword).Result;

                if (!success)
                {
                    throw new ResourceNotFoundException("Invalid user or password entered.");
                }

                var task = user.ChangePassword(request.NewPassword, _userManager);

                Task.WaitAll(task);
            }
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateUser request)
        {
            // We need to parse this manually because we told service stack not to with IRequiresRequestStream
            // https://code.google.com/p/servicestack/source/browse/trunk/Common/ServiceStack.Text/ServiceStack.Text/Controller/PathInfo.cs
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var id = new Guid(pathInfo.GetArgumentValue<string>(1));

            var dtoUser = request;

            var user = _userManager.GetUserById(id);

            var task = user.Name.Equals(dtoUser.Name, StringComparison.Ordinal) ? _userManager.UpdateUser(user) : _userManager.RenameUser(user, dtoUser.Name);

            Task.WaitAll(task);

            user.UpdateConfiguration(dtoUser.Configuration, _xmlSerializer);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Post(CreateUser request)
        {
            var dtoUser = request;

            var newUser = _userManager.CreateUser(dtoUser.Name).Result;

            newUser.UpdateConfiguration(dtoUser.Configuration, _xmlSerializer);
            
            var result = new DtoBuilder(Logger).GetDtoUser(newUser);

            return ToOptimizedResult(result);
        }
    }
}
