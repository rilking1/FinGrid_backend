using Microsoft.AspNetCore.Identity;

namespace FinGrid.Models
{
    public class User : IdentityUser
    {
        public bool IsBankSyncEnabled { get; set; } = false;
    }
}