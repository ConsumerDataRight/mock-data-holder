using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Repository.Entities
{
    public class Customer
    {
        public Customer()
        {
            this.CustomerId = Guid.NewGuid();
        }

        [Key]
        public Guid CustomerId { get; set; }
        [Required, MaxLength(8)]
        public string LoginId { get; set; }

        public string CustomerUType { get; set; }

        public Guid? PersonId { get; set; }
        public virtual Person Person { get; set; }

        public Guid? OrganisationId { get; set; }
        public virtual Organisation Organisation { get; set; }

        public virtual ICollection<Account> Accounts { get; set; }

        public string Name
        {
            get
            {
                if (this.Person != null && this.CustomerUType.Equals("person", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{this.Person.FirstName} {this.Person.LastName}".Trim();
                }

                if (this.Organisation != null)
                {
                    return this.Organisation.BusinessName;
                }

                return "";
            }
        }

        public string GivenName
        {
            get
            {
                if (this.Person != null && this.CustomerUType.Equals("person", StringComparison.OrdinalIgnoreCase))
                {
                    return this.Person.FirstName;
                }

                if (this.Organisation != null)
                {
                    return this.Organisation.AgentFirstName;
                }

                return "";
            }
        }

        public string FamilyName
        {
            get
            {
                if (this.Person != null && this.CustomerUType.Equals("person", StringComparison.OrdinalIgnoreCase))
                {
                    return this.Person.LastName;
                }

                if (this.Organisation != null)
                {
                    return this.Organisation.AgentLastName;
                }

                return "";
            }
        }

        public DateTime? LastUpdated
        {
            get
            {
                if (this.Person != null && this.CustomerUType.Equals("person", StringComparison.OrdinalIgnoreCase))
                {
                    return this.Person.LastUpdateTime;
                }

                if (this.Organisation != null)
                {
                    return this.Organisation.LastUpdateTime;
                }

                return null;
            }
        }
    }
}
