using QuantamStore.Webapi.Data;

namespace QuantamStore.Webapi.Services
{
    public class UserValidator
    {
        private readonly ApplicationDbContext _context;

        public UserValidator(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool EmailExists(string email, int? excludeUserId = null)
        {
            return _context.Users.Any(u => u.Email == email && u.Id != excludeUserId);
        }

        public bool UsernameExists(string username, int? excludeUserId = null)
        {
            return _context.Users.Any(u => u.Username == username && u.Id != excludeUserId);
        }
    }
}
