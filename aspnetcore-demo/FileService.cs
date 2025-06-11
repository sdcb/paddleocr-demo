using Amazon.S3;
using Amazon.S3.Model;
using System.Net;

namespace Sdcb.PaddleSharp.AspNetDemo;

public class FileService(IConfiguration config)
{
    public async Task<string> SaveFile(string fileName, byte[] fileContent)
    {
        if (config.GetValue<bool>("S3:Enabled"))
        {
            string accessKey = config["S3:AccessKey"] ?? throw new Exception("S3:AccessKey is required");
            string secret = config["S3:SecretKey"] ?? throw new Exception("S3:SecretKey is required");
            string bucketName = config["S3:BucketName"] ?? throw new Exception("S3:BucketName is required");
            string serviceUrl = config["S3:ServiceUrl"] ?? throw new Exception("S3:ServiceUrl is required");
            
            return await Upload(fileName, fileContent, accessKey, secret, bucketName, serviceUrl);
        }
        else
        {
            string file = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllBytes(file, fileContent);
            return file;
        }
    }

    private static async Task<string> Upload(string fileName, byte[] fileContent, string accessKey, string secret, string bucketName, string serviceUrl)
    {
        AmazonS3Client s3 = new(accessKey, secret, new AmazonS3Config
        {
            ForcePathStyle = true,
            ServiceURL = serviceUrl,
        });
        string ext = Path.GetExtension(fileName);
        string objectKey = $"{DateTime.Now:yyyy/MM/dd}/{fileName}-{DateTime.Now:HHmmss}.{ext}";
        PutObjectResponse resp = await s3.PutObjectAsync(new()
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = new MemoryStream(fileContent),
        });
        if (resp.HttpStatusCode != HttpStatusCode.OK) throw new Exception($"上传失败 {resp.HttpStatusCode}");

        string downloadUrl = s3.GetPreSignedURL(new GetPreSignedUrlRequest()
        {
            BucketName = bucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.AddHours(1),
        });

        return downloadUrl;
    }
}
