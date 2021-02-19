using System;

namespace Azure.DurableFunctions.Models
{
    public class EmployeePayload
    {
        public string EmployeeNumber { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime HireDate { get; set; }
        public decimal Salary { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }
    }
}
