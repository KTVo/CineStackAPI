using CineStackAPI.MVCS.Models.SignIn;
using CineStackAPI.MVCS.Repositories.Responses;
using CineStackAPI.MVCS.Services.Context;
using Microsoft.EntityFrameworkCore;

public sealed class UserRepository : IUserRepository
{
    private readonly RegistrationDbContext _dBcontext;

    public UserRepository(RegistrationDbContext context)
    {
        _dBcontext = context;
    }

    /// <summary>
    /// Validates that the required services are initialized and available for use.
    /// </summary>
    /// <returns></returns>
    public bool ValidateServices()
    {
        if (_dBcontext == null) { throw new ArgumentNullException("RegistrationDbContext is not initialized."); }

        return true;
    }

    /// <summary>
    /// Creates a new user in the database.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception> <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<SignUpResponseModel> AddUserAsync(ApplicationUserModel model)
    {
        // NULL CHECKS
        if (model == null) { throw new ArgumentNullException("User model is null."); }
        if (string.IsNullOrEmpty(model.UserName)) { throw new ArgumentNullException("Username is null!"); }
        if (string.IsNullOrEmpty(model.Email)) { throw new ArgumentNullException("Email is null!"); }
        if (string.IsNullOrEmpty(model.PasswordHash)) { throw new ArgumentNullException("HashedPassword is null!"); }
        if (string.IsNullOrEmpty(model.UserType)) { throw new ArgumentNullException("UserType is null!"); }


        try
        {

            // CHECK IF USER ALREADY EXISTS BY EMAIL
            ApplicationUserResponse existingUserEmail = await GetUserByEmailAsync(model.Email);

            // IF USER ALREADY EXISTS BY EMAIL, RETURN FAILURE RESPONSE
            if (existingUserEmail.User != null)
            {
                return new SignUpResponseModel
                {
                    IsSuccess = false,
                    Message = "User with this email already exists!"
                };
            }

            // IF QUERY FAILED, RETURN FAILURE RESPONSE
            if (existingUserEmail.IsSuccess == false)
            {
                return new SignUpResponseModel
                {
                    IsSuccess = false,
                    Message = existingUserEmail.Message
                };

            }


            // CHECK IF USER ALREADY EXISTS BY USERNAME
            ApplicationUserResponse existingUserUsername = await GetUserByUsernameAsync(model.UserName);

            // IF USER ALREADY EXISTS BY USERNAME, RETURN FAILURE RESPONSE
            if (existingUserUsername.User != null)
            {
                return new SignUpResponseModel
                {
                    IsSuccess = false,
                    Message = "User with this username already exists!"
                };
            }

            // IF QUERY FAILED, RETURN FAILURE RESPONSE
            if (existingUserUsername.IsSuccess == false)
            {
                return new SignUpResponseModel
                {
                    IsSuccess = false,
                    Message = existingUserUsername.Message
                };

            }

            // ADD USER TO DATABASE
            await _dBcontext.ApplicationUser.AddAsync(model);
            // SAVE CHANGES TO DATABASE
            await _dBcontext.SaveChangesAsync();

            return new SignUpResponseModel
            {
                IsSuccess = true,
                Message = "User registered successfully."
            };
        }
        catch (Exception ex)
        {
            return new SignUpResponseModel
            {
                IsSuccess = false,
                Message = $"Error occurred while adding user: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// VERFIES USER CREDENTIALS AND RETRIEVES USER FROM DATABASE BY EMAIL AND PASSWORD.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public async Task<SignInResponseModel> GetUserAsync(SignInRequestModel model)
    {
        if (string.IsNullOrEmpty(model.Email)) { throw new ArgumentNullException("Email is null or empty!"); }
        if (string.IsNullOrEmpty(model.Password)) { throw new ArgumentNullException("Password is null or empty!"); }

        try
        {
            ApplicationUserModel? user = null;

            // CHECK IF USER EXISTS BY EMAIL
            if (string.IsNullOrEmpty(model.Email) == false)
            {
                ApplicationUserResponse foundUserByEmail = await GetUserByEmailAsync(model.Email);
                user = foundUserByEmail.User;
            }

            // CHECK IF USER EXISTS BY USERNAME
            if (user == null && string.IsNullOrEmpty(model.UserName) == false)
            {
                ApplicationUserResponse foundUserByUsername = await GetUserByUsernameAsync(model.UserName);
                user = foundUserByUsername.User;
            }

            // IF USER NOT FOUND, RETURN FAILURE RESPONSE
            if (user == null)
            {
                return new SignInResponseModel
                {
                    IsSuccess = true,
                    Message = "User not found!"
                };
            }

            // CHECK IF USER EXISTS BY EMAIL AND PASSWORD OR USERNAME AND PASSWORD
            if (string.IsNullOrEmpty(model.Email) == false && string.IsNullOrEmpty(model.Password) == false)
            {
                user = await _dBcontext.ApplicationUser
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == model.Password);
            }
            else if (string.IsNullOrEmpty(model.UserName) == false && string.IsNullOrEmpty(model.Password) == false)
            {
                user = await _dBcontext.ApplicationUser
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserName == model.UserName && u.PasswordHash == model.Password);
            }

            // IF USER NOT FOUND, RETURN FAILURE RESPONSE
            if (user == null)
            {
                return new SignInResponseModel
                {
                    IsSuccess = true,
                    Message = "User not found!"
                };
            }
            
            // TODO: GENERATE JWT TOKEN HERE IF USER IS FOUND AND PASSWORD MATCHES

            // RETURN SUCCESS RESPONSE WITH USER DATA
            return new()
            {
                IsSuccess = true,
                Message = "User retrieved successfully.",
                User = user
            };
        }
        catch (Exception ex)
        {
            return new SignInResponseModel
            {
                IsSuccess = false,
                Message = $"Error occurred while retrieving user: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Retrieves a user from the database by their email address.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception> <summary>
    /// 
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<ApplicationUserResponse> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrEmpty(email)) { throw new ArgumentNullException("Email is null or empty!"); }

        try
        {
            ApplicationUserModel? user = await _dBcontext
                .ApplicationUser
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return new ApplicationUserResponse
                {
                    IsSuccess = true,
                    Message = "User not found!"
                };
            }

            return new()
            {
                IsSuccess = true,
                Message = "User retrieved successfully.",
                User = user
            };
        }
        catch (Exception ex)
        {
            return new ApplicationUserResponse
            {
                IsSuccess = false,
                Message = $"Error occurred while retrieving user: {ex.Message}"
            };
        }

    }
    
    /// <summary>
    /// Retrieves a user from the database by their username.
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
     public async Task<ApplicationUserResponse> GetUserByUsernameAsync(string username)
    {
        if (string.IsNullOrEmpty(username)) { throw new ArgumentNullException("Username is null or empty!"); }

        try
        {
            ApplicationUserModel? user = await _dBcontext.ApplicationUser
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == username);
            
            if (user == null)
            {
                return new ApplicationUserResponse
                {
                    IsSuccess = true,
                    Message = "User not found!"
                };
            }

            return new()
            {
                IsSuccess = true,
                Message = "User retrieved successfully.",
                User = user
            };
        }
        catch (Exception ex)
        {
            return new ApplicationUserResponse
            {
                IsSuccess = false,
                Message = $"Error occurred while retrieving user: {ex.Message}"
            };
        }

    }
}