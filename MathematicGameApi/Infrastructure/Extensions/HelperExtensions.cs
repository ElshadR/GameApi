using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace MathematicGameApi.Infrastructure.Extensions
{
    public static class HelperExtensions
    {
        public static void CreatePasswordHash(this string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        public static bool VerifyPasswordHash(this string password, byte[] storedHash, byte[] storedSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (storedHash.Length != 64)
                throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (storedSalt.Length != 128)
                throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }

            return true;
        }

        public static bool DeleteFile(this string name)
        {
            try
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Files", "Images", name);
                string fullPathCompress =
                    Path.Combine(Directory.GetCurrentDirectory(), "Files", "Images", "Compress", name);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    if (File.Exists(fullPathCompress))
                    {
                        File.Delete(fullPathCompress);
                    }

                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async static Task<List<string>> Save(this List<IFormFile> files, string root, params string[] folders)
        {
            if (files == null || files?.Count == 0)
                return null;

            List<string> result = new List<string>();

            foreach (var file in files)
            {
                var fileName = await Save(file, root, folders);
                result.Add(fileName);
            }

            return result;
        }

        public async static Task SaveResizeAndCompress(this string basePath, List<string> filesPath, string root,
            params string[] folders)
        {
            string path1 = Path.Combine(root);
            string folder = "";
            foreach (var item in folders)
            {
                folder = Path.Combine(folder, item);
            }

            path1 = Path.Combine(path1, folder);

            foreach (var file in filesPath)
            {
                var fileRoute = Path.Combine(basePath, file);
                var path = Path.Combine(path1, file);

                using (Image image = Image.Load(fileRoute))
                {
                    //resize start
                    var mod = ((decimal) image.Height) / (197m);
                    var imageWidth = (int) (image.Width / mod);
                    var imageHeight = (int) (image.Height / mod);
                    image.Mutate(x => x
                        .Resize(imageWidth, imageHeight));

                    var random = new Guid();
                    image.Save(path);
                    //end

                    //compress start
                    var newFile = new FileInfo(path);
                    var optimizer = new ImageOptimizer();
                    optimizer.Compress(newFile);
                    newFile.Refresh();
                    //end
                }
            }
        }
        /// <summary>
        /// file'i kaydetmek icin kullanilir
        /// </summary>
        /// <param name="file"></param>
        /// <param name="root"></param>
        /// <param name="folders"></param>
        /// <returns></returns>

        public async static Task<string> Save(this IFormFile file, string root, params string[] folders)
        {
            string path = Path.Combine(root);
            string folder = "";
            foreach (var item in folders)
            {
                folder = Path.Combine(folder, item);
            }

            path = Path.Combine(path, folder);
            string fileName = Guid.NewGuid().ToString() + file.FileName;
            string resultPath = Path.Combine(path, fileName);

            using (FileStream fileStream = new FileStream(resultPath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return fileName;
        }
        
        public static DateTime BakuDateNowToTurkeyDate()
        {
            return DateTime.Now.AddHours(-1);
        }
    }
}