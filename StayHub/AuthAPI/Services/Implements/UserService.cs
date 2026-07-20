using AutoMapper;
using AuthAPI.DTOs;
using AuthAPI.Models;
using AuthAPI.Repositories;
using AuthAPI.Helpers;
using System.Security.Cryptography;

namespace AuthAPI.Services.Implements
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHelper _passwordHelper;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IOtpCacheService _otpCacheService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHelper passwordHelper,
            IMapper mapper,
            ICloudinaryService cloudinaryService,
            IOtpCacheService otpCacheService,
            IEmailTemplateService emailTemplateService,
            IEmailService emailService,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHelper = passwordHelper;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
            _otpCacheService = otpCacheService;
            _emailTemplateService = emailTemplateService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<PaginationDTO<ReadUserDTO>> GetAllUsers(int page, int pageSize)
        {
            var (users, total) = await _userRepository.GetAllPaged(page, pageSize);
            var userDtos = _mapper.Map<List<ReadUserDTO>>(users);

            return new PaginationDTO<ReadUserDTO>
            {
                Data = userDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<ReadUserDTO?> GetUserById(int id)
        {
            var user = await _userRepository.GetById(id);
            return user == null ? null : _mapper.Map<ReadUserDTO>(user);
        }

        public async Task<ReadUserDTO?> GetUserByEmail(string email)
        {
            var user = await _userRepository.GetByEmail(email);
            return user == null ? null : _mapper.Map<ReadUserDTO>(user);
        }

        public async Task<AdminCreatedUserDTO> CreateUserByAdmin(CreateUserDTO createUserDto)
        {
            var existingUser = await _userRepository.GetByEmail(createUserDto.Email);
            if (existingUser != null)
                throw new InvalidOperationException("Email is already in use.");

            if (!string.IsNullOrWhiteSpace(createUserDto.PhoneNumber))
            {
                var existingPhoneUser = await _userRepository.GetByPhoneNumber(createUserDto.PhoneNumber);
                if (existingPhoneUser != null)
                    throw new InvalidOperationException("PhoneNumberExists");
            }

            var newUser = _mapper.Map<User>(createUserDto);
            var temporaryPassword = GenerateTemporaryPassword();

            if (createUserDto.AvatarFile != null && createUserDto.AvatarFile.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(createUserDto.AvatarFile, "StayHub_Avatars");

                if (uploadResult != null && uploadResult.SecureUrl != null)
                {
                    newUser.AvatarUrl = uploadResult.SecureUrl.ToString();
                }
            }

            newUser.PasswordHash = _passwordHelper.Hash(newUser, temporaryPassword);
            newUser.Provider = "Local";
            newUser.RequirePasswordChange = true;
            newUser.SecurityStamp = Guid.NewGuid().ToString();

            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            newUser.LocPrivacy = true;
            newUser.MomentPrivacy = true;

            newUser.Roles ??= new List<Models.Role>();

            if (createUserDto.RoleIds != null && createUserDto.RoleIds.Any())
            {
                foreach (var roleId in createUserDto.RoleIds)
                {
                    var role = await _roleRepository.GetById(roleId);
                    if (role != null) newUser.Roles.Add(role);
                }
            }
            else
            {
                var defaultRole = await _roleRepository.GetByName("Customer");
                if (defaultRole != null) newUser.Roles.Add(defaultRole);
            }

            await _userRepository.Add(newUser);
            bool? credentialsEmailSent = null;

            if (createUserDto.SendCredentialsEmail)
            {
                try
                {
                    await SendTemporaryCredentialsEmail(
                        newUser.Email,
                        newUser.FullName,
                        temporaryPassword);
                    credentialsEmailSent = true;
                }
                catch (Exception ex)
                {
                    credentialsEmailSent = false;
                    _logger.LogError(
                        ex,
                        "User {UserId} was created, but the temporary credentials email could not be sent.",
                        newUser.Id);
                }
            }

            return new AdminCreatedUserDTO
            {
                User = _mapper.Map<ReadUserDTO>(newUser),
                TemporaryPassword = temporaryPassword,
                CredentialsEmailSent = credentialsEmailSent
            };
        }

        private Task SendTemporaryCredentialsEmail(
            string email,
            string fullName,
            string temporaryPassword)
        {
            const string subject = "Your StayHub account has been created";
            var body = _emailTemplateService.GenerateTemporaryCredentialsEmailBody(fullName, email, temporaryPassword);

            return _emailService.SendEmailAsync(email, subject, body);
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string special = "@$!%*?&";
            const string all = upper + lower + digits + special;

            var characters = new char[14];
            characters[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            characters[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            characters[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            characters[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

            for (var index = 4; index < characters.Length; index++)
            {
                characters[index] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            RandomNumberGenerator.Shuffle(characters.AsSpan());
            return new string(characters);
        }

        public async Task<bool> UpdateUserProfile(int id, UpdateUserDTO updateUserDto)
        {
            var existingUser = await _userRepository.GetById(id);
            if (existingUser == null) return false;

            if (existingUser.Roles.Any(r => r.Name == "Customer"))
            {
                throw new InvalidOperationException("Cannot edit Customer accounts.");
            }

            if (existingUser.Roles.Any(r => r.Name == "Admin"))
            {
                throw new InvalidOperationException("Cannot edit or block other Admin accounts.");
            }

            if (!string.IsNullOrWhiteSpace(updateUserDto.PhoneNumber) && updateUserDto.PhoneNumber != existingUser.PhoneNumber)
            {
                var existingPhoneUser = await _userRepository.GetByPhoneNumber(updateUserDto.PhoneNumber);
                if (existingPhoneUser != null)
                {
                    throw new InvalidOperationException("PhoneNumberExists");
                }
            }

            _mapper.Map(updateUserDto, existingUser);

            if (updateUserDto.AvatarFile != null && updateUserDto.AvatarFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingUser.AvatarUrl))
                {
                    var publicId = _cloudinaryService.ExtractPublicIdFromUrl(existingUser.AvatarUrl);
                    if (publicId != null)
                    {
                        await _cloudinaryService.DeleteImageAsync(publicId);
                    }
                }

                var uploadResult = await _cloudinaryService.UploadImageAsync(updateUserDto.AvatarFile, "StayHub_Avatars");

                if (uploadResult != null && uploadResult.SecureUrl != null)
                {
                    existingUser.AvatarUrl = uploadResult.SecureUrl.ToString();
                }
            }

            existingUser.SecurityStamp = Guid.NewGuid().ToString();
            existingUser.UpdatedAt = DateTime.UtcNow;

            existingUser.Roles ??= new List<Models.Role>();

            if (updateUserDto.RoleIds != null)
            {
                existingUser.Roles.Clear();
                foreach (var roleId in updateUserDto.RoleIds)
                {
                    var role = await _roleRepository.GetById(roleId);
                    if (role != null) existingUser.Roles.Add(role);
                }
            }


            if (updateUserDto.Status == "Blocked")
            {
                await _otpCacheService.RevokeUserSessionAsync(id, TimeSpan.FromMinutes(60));
            }

            await _userRepository.Update(id, existingUser);

            await _refreshTokenRepository.DeleteAllByUserId(id);

            return true;
        }

        public async Task<bool> DeleteUser(int id)
        {
            throw new InvalidOperationException("Deleting user accounts is not allowed by business rules. Please block the account instead.");
        }

        public async Task<PaginationDTO<UserSearchResultDto>> SearchUsersAsync(string query, int page, int pageSize, string? role = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new PaginationDTO<UserSearchResultDto>
                {
                    Data = new List<UserSearchResultDto>(),
                    Total = 0,
                    TotalPages = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }

            var (users, total) = await _userRepository.SearchPagedAsync(query, page, pageSize, role);
            var userDtos = _mapper.Map<List<UserSearchResultDto>>(users);

            return new PaginationDTO<UserSearchResultDto>
            {
                Data = userDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<UserProfileDto> GetPublicProfileAsync(int id)
        {
            var user = await _userRepository.GetById(id);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {id} was not found.");
            }

            return _mapper.Map<UserProfileDto>(user);
        }

        public async Task UpdatePrivacyAsync(int userId, PrivacySettingsDto dto)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            user.LocPrivacy = dto.LocPrivacy;
            user.MomentPrivacy = dto.MomentPrivacy;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.Update(userId, user);
        }

        public async Task<IEnumerable<UserSearchResultDto>> GetUsersBatchAsync(List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return Enumerable.Empty<UserSearchResultDto>();
            }

            var uniqueIds = userIds.Distinct().ToList();
            var users = await _userRepository.GetUsersByIdsAsync(uniqueIds);


            return _mapper.Map<IEnumerable<UserSearchResultDto>>(users);
        }

        public async Task<bool> ChangeUserStatusAsync(int id, string newStatus)
        {
            var user = await _userRepository.GetById(id);
            if (user == null) return false;

            if (user.Roles.Any(r => r.Name == "Admin"))
            {
                throw new InvalidOperationException("Cannot block or change the status of an Admin account.");
            }

            user.Status = newStatus;
            user.UpdatedAt = DateTime.UtcNow;

            if (newStatus == "Blocked")
            {
                user.SecurityStamp = Guid.NewGuid().ToString();

                await _refreshTokenRepository.DeleteAllByUserId(id);

                await _otpCacheService.RevokeUserSessionAsync(id, TimeSpan.FromMinutes(60));
            }

            await _userRepository.Update(id, user);

            try
            {
                string subject = newStatus == "Blocked" ? "Your StayHub account has been blocked" : "Your StayHub account has been activated";
                string body = newStatus == "Blocked" 
                    ? _emailTemplateService.GenerateAccountBlockedEmailBody(user.FullName)
                    : _emailTemplateService.GenerateAccountActivatedEmailBody(user.FullName);
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status change email to user {UserId}.", user.Id);
            }

            return true;
        }

        public async Task<PaginationDTO<ReadUserDTO>> FilterUsersAsync(UserFilterDTO filter)
        {
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 10;

            var (users, total) = await _userRepository.FilterPagedAsync(filter);

            var userDtos = _mapper.Map<List<ReadUserDTO>>(users);

            return new PaginationDTO<ReadUserDTO>
            {
                Data = userDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize),
                CurrentPage = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<UserProfileResponseDto?> GetUserProfileAsync(int userId)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null) return null;
            return _mapper.Map<UserProfileResponseDto>(user);
        }

        public async Task<CustomerDemographicsDTO> GetCustomerDemographicsAsync(
            DateTime? from,
            DateTime? to,
            string granularity)
        {
            ValidateAnalyticsDateRange(from, to);
            granularity = NormalizeGranularity(granularity);

            var customers = await _userRepository.GetAllCustomersAsync();
            var total = customers.Count;

            var periodFrom = from ?? DateTime.UtcNow.AddDays(-30);
            var periodTo = to ?? DateTime.UtcNow;

            var newInPeriod = customers.Count(c =>
                c.CreatedAt.HasValue &&
                c.CreatedAt.Value >= periodFrom &&
                c.CreatedAt.Value <= periodTo);

            var activeThreshold = DateTime.UtcNow.AddDays(-30);
            var activeCount = customers.Count(c =>
                c.Status == "Active" &&
                (c.LastOnline >= activeThreshold || c.CreatedAt >= activeThreshold));

            return new CustomerDemographicsDTO
            {
                TotalCustomers = total,
                ActiveCustomers = activeCount,
                InactiveCustomers = customers.Count(c => c.Status != "Active"),
                NewCustomersInPeriod = newInPeriod,
                ByGender = BuildLabelCounts(customers, total, c => string.IsNullOrWhiteSpace(c.Gender) ? "Unknown" : c.Gender!),
                ByProvider = BuildLabelCounts(customers, total, c => string.IsNullOrWhiteSpace(c.Provider) ? "Unknown" : c.Provider!),
                ByStatus = BuildLabelCounts(customers, total, c => string.IsNullOrWhiteSpace(c.Status) ? "Unknown" : c.Status!),
                ByAgeGroup = BuildLabelCounts(customers, total, c => GetAgeGroup(c.DateOfBirth)),
                RegistrationTrend = BuildRegistrationTrend(customers, periodFrom, periodTo, granularity)
            };
        }

        public async Task<List<ReadUserDTO>> GetCustomersByBirthdayMonthAsync(int month)
        {
            var customers = await _userRepository.GetAllCustomersAsync();
            var birthdayCustomers = customers.Where(c => c.DateOfBirth.HasValue && c.DateOfBirth.Value.Month == month).ToList();
            return _mapper.Map<List<ReadUserDTO>>(birthdayCustomers);
        }

        public async Task<CustomerListAnalyticsDTO> GetCustomersForAnalyticsAsync(string? search, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 20;

            var (users, total) = await _userRepository.GetCustomersPagedAsync(search, page, pageSize);

            return new CustomerListAnalyticsDTO
            {
                Total = total,
                Customers = users.Select(u => new CustomerSummaryDTO
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    AvatarUrl = u.AvatarUrl,
                    Provider = u.Provider,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    Status = u.Status,
                    LastOnline = u.LastOnline,
                    CreatedAt = u.CreatedAt
                }).ToList()
            };
        }

        public async Task<PlatformUserStatsDTO> GetPlatformUserStatsAsync(
            DateTime? from,
            DateTime? to,
            string granularity)
        {
            ValidateAnalyticsDateRange(from, to);
            granularity = NormalizeGranularity(granularity);

            var users = await _userRepository.GetAll();
            var total = users.Count;

            var periodFrom = from ?? DateTime.UtcNow.AddDays(-30);
            var periodTo = to ?? DateTime.UtcNow;

            var newInPeriod = users.Count(u =>
                u.CreatedAt.HasValue &&
                u.CreatedAt.Value >= periodFrom &&
                u.CreatedAt.Value <= periodTo);

            var activeThreshold = DateTime.UtcNow.AddDays(-30);
            var onlineThreshold = DateTime.UtcNow.AddDays(-7);

            var activeCount = users.Count(u =>
                u.Status == "Active" &&
                (u.LastOnline >= activeThreshold || u.CreatedAt >= activeThreshold));

            var onlineRecently = users.Count(u =>
                u.LastOnline.HasValue && u.LastOnline.Value >= onlineThreshold);

            int CountByRole(string role) =>
                users.Count(u => u.Roles.Any(r => r.Name == role));

            var customers = CountByRole("Customer");
            var managers = CountByRole("Manager");
            var staff = CountByRole("Staff");
            var admins = CountByRole("Admin");

            var byRole = new List<LabelCountDTO>
            {
                BuildSingleLabel("Customer", customers, total),
                BuildSingleLabel("Manager", managers, total),
                BuildSingleLabel("Staff", staff, total),
                BuildSingleLabel("Admin", admins, total)
            }.Where(x => x.Count > 0).OrderByDescending(x => x.Count).ToList();

            return new PlatformUserStatsDTO
            {
                TotalUsers = total,
                ActiveUsers = activeCount,
                InactiveUsers = users.Count(u => u.Status != "Active"),
                NewUsersInPeriod = newInPeriod,
                OnlineRecently = onlineRecently,
                TotalCustomers = customers,
                TotalManagers = managers,
                TotalStaff = staff,
                TotalAdmins = admins,
                ByRole = byRole
            };
        }

        // Trong UserService.cs
        public async Task<string?> GetFcmTokenAsync(int userId)
        {
            return await _userRepository.GetFcmTokenAsync(userId);
        }

        // Thêm hàm này vào AuthAPI.Services.Implements.UserService
        public async Task<bool> UpdateFcmTokenAsync(int userId, string fcmToken)
        {
            var existingUser = await _userRepository.GetById(userId);
            if (existingUser == null) return false;

            existingUser.FcmToken = fcmToken;
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _userRepository.Update(userId, existingUser);

            return true;
        }

        private static LabelCountDTO BuildSingleLabel(string label, int count, int total)
        {
            return new LabelCountDTO
            {
                Label = label,
                Count = count,
                Percentage = total > 0 ? Math.Round(count * 100m / total, 2) : 0
            };
        }

        private static void ValidateAnalyticsDateRange(DateTime? from, DateTime? to)
        {
            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                throw new ArgumentException("Start date must be before or equal to end date.");
            }

            if (from.HasValue && to.HasValue && (to.Value - from.Value).TotalDays > 366)
            {
                throw new ArgumentException("Date range cannot exceed 366 days.");
            }
        }

        private static string NormalizeGranularity(string granularity)
        {
            var normalized = (granularity ?? "day").Trim().ToLowerInvariant();
            return normalized is "day" or "week" or "month" ? normalized : "day";
        }

        private static string GetAgeGroup(DateOnly? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return "Unknown";

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - dateOfBirth.Value.Year;
            if (dateOfBirth.Value > today.AddYears(-age)) age--;

            return age switch
            {
                < 18 => "Under 18",
                <= 24 => "18-24",
                <= 34 => "25-34",
                <= 44 => "35-44",
                <= 54 => "45-54",
                _ => "55+"
            };
        }

        private static List<LabelCountDTO> BuildLabelCounts<T>(
            List<T> items,
            int total,
            Func<T, string> labelSelector)
        {
            return items
                .GroupBy(labelSelector)
                .Select(g => new LabelCountDTO
                {
                    Label = g.Key,
                    Count = g.Count(),
                    Percentage = total > 0 ? Math.Round(g.Count() * 100m / total, 2) : 0
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        private static List<TimeSeriesPointDTO> BuildRegistrationTrend(
            List<User> customers,
            DateTime from,
            DateTime to,
            string granularity)
        {
            var filtered = customers
                .Where(c => c.CreatedAt.HasValue && c.CreatedAt.Value >= from && c.CreatedAt.Value <= to)
                .ToList();

            return filtered
                .GroupBy(c => FormatPeriod(c.CreatedAt!.Value, granularity))
                .Select(g => new TimeSeriesPointDTO { Period = g.Key, Count = g.Count() })
                .OrderBy(x => x.Period)
                .ToList();
        }

        private static string FormatPeriod(DateTime date, string granularity)
        {
            return granularity switch
            {
                "week" => $"{date.Year}-W{System.Globalization.ISOWeek.GetWeekOfYear(date):D2}",
                "month" => date.ToString("yyyy-MM"),
                _ => date.ToString("yyyy-MM-dd")
            };
        }
    }
}
