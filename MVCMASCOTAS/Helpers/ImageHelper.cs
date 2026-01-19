using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Web;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Helper para manejo de imágenes
    /// Conversión de HttpPostedFileBase a byte[] y viceversa
    /// Redimensionamiento de imágenes
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// Convierte un HttpPostedFileBase a byte[] para almacenar en VARBINARY(MAX)
        /// </summary>
        public static byte[] ConvertImageToByteArray(HttpPostedFileBase imageFile)
        {
            if (imageFile == null || imageFile.ContentLength == 0)
                return null;

            using (var binaryReader = new BinaryReader(imageFile.InputStream))
            {
                return binaryReader.ReadBytes(imageFile.ContentLength);
            }
        }

        /// <summary>
        /// Convierte byte[] a Base64 string para mostrar en vistas
        /// </summary>
        public static string ConvertByteArrayToBase64(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            return Convert.ToBase64String(imageBytes);
        }

        /// <summary>
        /// Obtiene el data URI completo para usar en atributo src de img
        /// </summary>
        public static string GetImageDataUri(byte[] imageBytes, string contentType = "image/jpeg")
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            string base64 = ConvertByteArrayToBase64(imageBytes);
            return $"data:{contentType};base64,{base64}";
        }

        /// <summary>
        /// Redimensiona una imagen manteniendo la proporción
        /// </summary>
        public static byte[] ResizeImage(byte[] imageBytes, int maxWidth, int maxHeight)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            using (var ms = new MemoryStream(imageBytes))
            using (var image = Image.FromStream(ms))
            {
                // Calcular nuevas dimensiones manteniendo proporción
                int newWidth = image.Width;
                int newHeight = image.Height;

                if (image.Width > maxWidth || image.Height > maxHeight)
                {
                    double ratioX = (double)maxWidth / image.Width;
                    double ratioY = (double)maxHeight / image.Height;
                    double ratio = Math.Min(ratioX, ratioY);

                    newWidth = (int)(image.Width * ratio);
                    newHeight = (int)(image.Height * ratio);
                }

                // Crear imagen redimensionada
                using (var bitmap = new Bitmap(newWidth, newHeight))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;

                    graphics.DrawImage(image, 0, 0, newWidth, newHeight);

                    using (var outputStream = new MemoryStream())
                    {
                        bitmap.Save(outputStream, ImageFormat.Jpeg);
                        return outputStream.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Valida que el archivo sea una imagen válida
        /// </summary>
        public static bool IsValidImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return false;

            // Validar extensión
            string[] validExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
            string extension = Path.GetExtension(file.FileName).ToLower();

            if (!Array.Exists(validExtensions, ext => ext == extension))
                return false;

            // Validar tipo MIME
            string[] validMimeTypes = { "image/jpeg", "image/png", "image/gif" };
            if (!Array.Exists(validMimeTypes, mime => mime == file.ContentType.ToLower()))
                return false;

            try
            {
                // Intentar abrir como imagen
                using (var image = Image.FromStream(file.InputStream))
                {
                    // Si llega aquí, es una imagen válida
                    file.InputStream.Position = 0; // Resetear posición del stream
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene el tamaño del archivo en MB
        /// </summary>
        public static double GetFileSizeInMB(HttpPostedFileBase file)
        {
            if (file == null)
                return 0;

            return (double)file.ContentLength / (1024 * 1024);
        }

        /// <summary>
        /// Valida tamaño máximo de imagen
        /// </summary>
        public static bool IsFileSizeValid(HttpPostedFileBase file, int maxSizeMB)
        {
            if (file == null)
                return false;

            double fileSizeMB = GetFileSizeInMB(file);
            return fileSizeMB <= maxSizeMB;
        }
    }
}