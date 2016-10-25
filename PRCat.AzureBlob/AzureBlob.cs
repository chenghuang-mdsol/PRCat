using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace PRCat.AzureBlob
{
    public class AzureBlob
    {
        public string UploadBlobFromStream(Stream stream, string blobName)
        {
            var blobContainer = ConfigurationManager.AppSettings["AzureBlob.Container"];
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureBlob.ConnectionString"));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(blobContainer);

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            blockBlob.UploadFromStream(stream);

            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTime.UtcNow.AddDays(60),
                Permissions = SharedAccessBlobPermissions.Read
            };

            //Generate the shared access signature on the blob, setting the constraints directly on the signature.
            string sasBlobToken = blockBlob.GetSharedAccessSignature(sasConstraints);

            return blockBlob.Uri + sasBlobToken;

        }

        
    }
}
