using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegratedCacheDemo
{
    public class SampleCustomer
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        [JsonProperty(PropertyName = "postalcode")]
        public string PostalCode { get; set; }

        [JsonProperty(PropertyName = "region")]
        public string Region { get; set; }

        [JsonProperty(PropertyName = "myPartitionKey")]
        public string MyPartitionKey { get; set; }

        [JsonProperty(PropertyName = "userDefinedId")]
        public int UserDefinedId { get; set; }

        public static List<SampleCustomer> GenerateManyCustomers(string partitionKeyValue, int number)
        {
            //Generate fake customer data.
            Bogus.Faker<SampleCustomer> customerGenerator = new Bogus.Faker<SampleCustomer>().Rules((faker, customer) =>
            {
                customer.Id = Guid.NewGuid().ToString();
                customer.Name = faker.Name.FullName();
                customer.City = faker.Person.Address.City.ToString();
                customer.Region = faker.Person.Address.State.ToString();
                customer.PostalCode = faker.Person.Address.ZipCode.ToString();
                customer.MyPartitionKey = partitionKeyValue;
                customer.UserDefinedId = faker.Random.Int(0, 1000);
            });

            return customerGenerator.Generate(number);
        }

        public static List<SampleCustomer> GenerateSingleCustomer(string partitionKeyValue, string id, string name)
        {
            // Generate a new custom item
            Bogus.Faker<SampleCustomer> customerGenerator = new Bogus.Faker<SampleCustomer>().Rules((faker, customer) =>
            {
                customer.Id = id; //explicitly set this value based on user input
                customer.Name = name; //explicitly set this value based on user input
                customer.City = faker.Person.Address.City.ToString();
                customer.Region = faker.Person.Address.State.ToString();
                customer.PostalCode = faker.Person.Address.ZipCode.ToString();
                customer.MyPartitionKey = partitionKeyValue;
                customer.UserDefinedId = faker.Random.Int(0, 1000);
            });

            return customerGenerator.Generate(1);
        }
    }
}
