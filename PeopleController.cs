using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Program_3_Website_Storage.Models;
using System.Web.Mvc;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos.Table;
using System.IO;
using System.Text;
using CloudTable = Microsoft.Azure.Cosmos.Table.CloudTable;
using System.Configuration;

namespace Program_3_Website_Storage.Controllers
{
    public class PeopleController : Controller
    {
        static string blobConnectionString = ConfigurationManager.ConnectionStrings["blobConnectionString"].ConnectionString;
        static string tableConnectionString = ConfigurationManager.ConnectionStrings["tableConnectionString"].ConnectionString;
        static string tableURL = "https://simon.table.cosmos.azure.com";
        static string peopleURL = "https://css490.blob.core.windows.net/lab4/input.txt";
        static BlobServiceClient blobClient = new BlobServiceClient(blobConnectionString);
        static string containerName = "program4container";
        static string blobName = "peopleblob";
        static BlobContainerClient container = new BlobContainerClient(blobConnectionString, containerName);
        static CloudStorageAccount storageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(tableConnectionString);
        static CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
        static CloudTable table = cloudTableClient.GetTableReference("peopletable");

        public ActionResult PersonSearch()
        {
            ViewBag.Title = "Search Page";

            return View();
        }
        public ActionResult handleLoad(ViewModel model)
        {
            getPeople();
            model.message = "Database Loaded";
            return View("PersonSearch", model);
        }

        public ActionResult handleClear(ViewModel model)
        {
            table.DeleteIfExists();
            container.GetBlobClient(blobName).DeleteIfExists();
            model.message = "Database Cleared";
            return View("PersonSearch", model);
        }

        public ActionResult handleQuery(ViewModel model)
        {
            List<DynamicTableEntity> query;
            if (model.person.PartitionKey == null)
            {
                query = table.CreateQuery<DynamicTableEntity>().Where(x => x.RowKey == model.person.RowKey).ToList();
            }
            else if(model.person.RowKey==null)
            {
                query = table.CreateQuery<DynamicTableEntity>().Where(x => x.PartitionKey == model.person.PartitionKey).ToList();
            }
            else
            {
                query = table.CreateQuery<DynamicTableEntity>().Where(x => x.PartitionKey==model.person.PartitionKey && x.RowKey == model.person.RowKey).ToList();
            }


            List<Person> people = new List<Person>();
            foreach (DynamicTableEntity result in query)
            {
                Person person = new Person();
                person.PartitionKey = result.PartitionKey;
                person.RowKey = result.RowKey;
                person.attributes = result.Properties;
                people.Add(person);
            }
            model.people = people;
            model.message = "Database Queried";
            return View("PersonSearch", model);
        }


        static void handleBlob(string text)
        {
            var content = Encoding.UTF8.GetBytes(text);
            using (var ms = new MemoryStream(content))
                container.GetBlobClient(blobName).Upload(ms, overwrite: true);
        }
        
        static void handleTable(string text)
        {
            table.CreateIfNotExists();
            People people = new People();
            people.setAttributes(text);
            
            foreach(Person person in people.people)
            {
                var dte = new DynamicTableEntity(person.PartitionKey, person.RowKey);
                dte.Properties = person.attributes;
                table.Execute(TableOperation.InsertOrReplace(dte));
            }
        }
        static Boolean getPeople()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(peopleURL);
            string result = handleResponse(client, -1);
            if (result.Length != 0)
            {
                handleBlob(result);
                handleTable(result);
                return true;
            }
            return false;
        }
        static string handleResponse(HttpClient client, int retry)
        {
            if (retry > 0)
                System.Threading.Thread.Sleep(retry * 1000);
            HttpResponseMessage response = client.GetAsync("").Result;
            if (!response.IsSuccessStatusCode)
            {
                int statusCode = (int)response.StatusCode;
                if (statusCode / 100 == 5 && retry < 8)
                {
                    if (retry < 1)
                        retry++;
                    else
                        retry *= 2;
                    return handleResponse(client, retry);
                }
                Console.WriteLine("API request failed. Status code: " + response.StatusCode);
                return "";
            }
            return response.Content.ReadAsStringAsync().Result;
        }
    }

    

}
