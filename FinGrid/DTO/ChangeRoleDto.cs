namespace FinGrid.DTO
{
    public class ChangeRoleDto
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }


        public List<string> AllRoles { get; set; }


        public IList<string> UserRoles { get; set; }

        public ChangeRoleDto()
        {
            AllRoles = new List<string>();
            UserRoles = new List<string>();
        }
    }
}