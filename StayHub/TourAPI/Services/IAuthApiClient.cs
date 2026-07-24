using System.Collections.Generic;
using System.Threading.Tasks;
using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface IAuthApiClient
    {
        Task<List<AuthCustomerSummary>> GetUsersByRoleAsync(string role);
    }
}
