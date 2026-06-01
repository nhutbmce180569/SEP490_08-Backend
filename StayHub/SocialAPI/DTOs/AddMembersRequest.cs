using System.Collections.Generic;

namespace SocialAPI.DTOs
{
    public class AddMembersRequest
    {
        public List<int> UserIds { get; set; } = new List<int>();
    }
}