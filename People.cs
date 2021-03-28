using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Program_3_Website_Storage.Models
{
    public class ViewModel
    {
        public Person person { get; set; }
        public List<Person> people { get; set; }
        public string message { get; set; } = "";
    }
    public class Person
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public IDictionary<string, EntityProperty> attributes = new Dictionary<string, EntityProperty>();

    }

    public class People 
    {
        public List<Person> people = new List<Person>();

        public string fixSpaces(string attributeString)
        {
            StringBuilder sb = new StringBuilder();
            for (int i=0; i<attributeString.Length; i++)
            {
                if (i == 0 || !(attributeString[i] == ' ' && attributeString[i - 1] == ' '))
                    sb.Append(attributeString[i]);
            }
            return sb.ToString();
        }
        public void setAttributes(string attributeString)
        {
            attributeString = fixSpaces(attributeString);
            if (attributeString.EndsWith("\n"))
                attributeString = attributeString.Remove(attributeString.Length - 1, 1);
            string[] peopleAttributes = attributeString.Split('\n');
            foreach (string personWhole in peopleAttributes)
            {
                if (personWhole != "") {
                    string personWholeFixed = personWhole;
                    if (personWholeFixed.EndsWith("\r"))
                        personWholeFixed = personWholeFixed.Remove(personWholeFixed.Length - 1, 1);
                    string[] personBroken = personWholeFixed.Split(null);
                    Person tempPerson = new Person();
                    tempPerson.RowKey = personBroken[0];
                    tempPerson.PartitionKey = personBroken[1];
                    for (int i = 2; i < personBroken.Length; i++)
                    {
                        string[] attribute = personBroken[i].Split('=');
                        //this was done because "id" is a reserved keyword in azure, and queries are case insensitive. So the id property, if left as is, would not be added
                        if (attribute[0] == "id")
                            attribute[0] = "_id";
                        tempPerson.attributes.Add(attribute[0], new EntityProperty(attribute[1]));
                    }
                    people.Add(tempPerson);
                }
                
            }
        }
    }
}