using System;
using System.Collections.Generic;
using PhoneStore.Models;

namespace PhoneStore.ViewModels
{
    public class CustomerFilterViewModel
    {
        public string? SearchString { get; set; } = string.Empty;
        public int? MembershipId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public List<Customer> Customers { get; set; } = new List<Customer>();
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int CustomersWithOrders { get; set; }
    }
}
