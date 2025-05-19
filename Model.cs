using System.Xml.Linq;

namespace SimpletextingAPI.Models {
     public class User
     {
        public string? ContactPhone { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Office { get;set; }
        public List<ContactList> Lists { get; set; } = new List<ContactList>();
        public List<string> ListNames { get; set; } = new List<string>();

        public string ListString
        {
            get
            {
                return string.Join(",", ListNames.OrderBy(x => x));
            }
        }
    }

    public class ContactList
    {
        private string? _listId;
        public string? ListId {
            get { return _listId ?? Id; }
            set { _listId = value; }
        }
        public string? Id { get; set; }
        public required string Name { get; set; }
    }

    public class UserApiResponse
    {
        public List<User>? Content { get; set; }
        public int TotalPages { get; set; }
        public long TotalElements { get; set; }
    }
    public class ListApiResponse
    {
        public List<ContactList>? Content { get; set; }
        public int TotalPages { get; set; }
        public long TotalElements { get; set; }
    }
}