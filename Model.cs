namespace SimpletextingAPI.Models {
     public class User
    {
       public string? ContactPhone { get; set; }
       public string? FirstName { get; set; }
       public string? LastName { get; set; }
       public string? Email { get; set; }
    }

    public class ApiResponse
    {
        public List<User>? Contacts { get; set; }
        public int TotalPages { get; set; }
        public long TotalElements { get; set; }
    }
}