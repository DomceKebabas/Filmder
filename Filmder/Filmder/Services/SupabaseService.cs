namespace Filmder.Services;

using Supabase;

using Microsoft.AspNetCore.Http;



    public class SupabaseService
    {
        private readonly string _url;
        private readonly string _key;
        private readonly string _profilePicturesBucket;

        public SupabaseService(IConfiguration config)
        {
            _url = config["Supabase:Url"];
            _key = config["Supabase:Key"];
            _profilePicturesBucket = config["Supabase:Buckets:ProfilePictures"];
        }

        private async Task<Client> CreateClientAsync()
        {
            var client = new Client(_url, _key);
            await client.InitializeAsync();
            return client;
        }

        public async Task<string?> UploadProfilePictureAsync(string userId, IFormFile file)
        {
            var client = await CreateClientAsync();
            var storage = client.Storage.From(_profilePicturesBucket);

            var extension = Path.GetExtension(file.FileName).ToLower();

            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => throw new InvalidOperationException("Unsupported image type")
            };

            var fileName = $"{userId}/profile{extension}";

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            await storage.Upload(
                bytes,
                fileName,
                new Supabase.Storage.FileOptions
                {
                    Upsert = true,
                    ContentType = contentType
                });

            return storage.GetPublicUrl(fileName);
        }

        public async Task DeleteProfilePictureByUrlAsync(string publicUrl)
        {
            var client = await CreateClientAsync();
            var storage = client.Storage.From(_profilePicturesBucket);

            var path = ExtractStoragePath(publicUrl);
            await storage.Remove(path);
        }

        private string ExtractStoragePath(string publicUrl)
        {
            var uri = new Uri(publicUrl);
            var segments = uri.AbsolutePath.Split('/');

            // /storage/v1/object/public/{bucket}/{path...}
            var index = Array.IndexOf(segments, "public") + 2;
            return string.Join("/", segments.Skip(index));
        }
    }



