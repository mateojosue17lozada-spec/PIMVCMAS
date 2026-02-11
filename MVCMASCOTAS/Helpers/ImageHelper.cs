using System;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Helper optimizado para manejo seguro de imágenes
    /// Funcionalidades: Conversión, Validación, Redimensionamiento, Compresión
    /// </summary>
    public static class ImageHelper
    {
        #region CONSTANTES

        // ⚡ Configuraciones desde Web.config
        private static readonly int MaxImageSizeMB = int.Parse(ConfigurationManager.AppSettings["TamañoMaximoImagenMB"] ?? "5");
        private static readonly string[] ValidExtensions = (ConfigurationManager.AppSettings["ExtensionesImagenesPermitidas"] ?? ".jpg,.jpeg,.png,.gif").Split(',');

        private static readonly string[] ValidMimeTypes = { "image/jpeg", "image/jpg", "image/png", "image/gif" };

        // Tamaños estándar para redimensionamiento
        public const int THUMBNAIL_SIZE = 150;
        public const int MEDIUM_SIZE = 400;
        public const int LARGE_SIZE = 800;

        #endregion

        #region CONVERSIÓN DE IMÁGENES

        /// <summary>
        /// Convierte un HttpPostedFileBase a byte[] para almacenar en BD
        /// </summary>
        public static byte[] ConvertImageToByteArray(HttpPostedFileBase imageFile)
        {
            if (imageFile == null || imageFile.ContentLength == 0)
                return null;

            try
            {
                // Asegurar que el stream esté en posición 0
                if (imageFile.InputStream.CanSeek)
                {
                    imageFile.InputStream.Position = 0;
                }

                using (var binaryReader = new BinaryReader(imageFile.InputStream))
                {
                    byte[] imageBytes = binaryReader.ReadBytes(imageFile.ContentLength);

                    // Restaurar posición del stream si es necesario
                    if (imageFile.InputStream.CanSeek)
                    {
                        imageFile.InputStream.Position = 0;
                    }

                    return imageBytes;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageHelper] Error en ConvertImageToByteArray: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convierte byte[] a Base64 string para mostrar en vistas
        /// </summary>
        public static string ConvertByteArrayToBase64(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            try
            {
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageHelper] Error en ConvertByteArrayToBase64: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene el data URI completo para usar en atributo src de img
        /// </summary>
        public static string GetImageDataUri(byte[] imageBytes, string contentType = "image/jpeg")
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            try
            {
                string base64 = ConvertByteArrayToBase64(imageBytes);
                return string.IsNullOrEmpty(base64) ? null : $"data:{contentType};base64,{base64}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageHelper] Error en GetImageDataUri: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Convierte Base64 a byte array
        /// </summary>
        public static byte[] ConvertBase64ToByteArray(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                return null;

            try
            {
                // Remover data URI si existe
                if (base64String.Contains(","))
                {
                    base64String = base64String.Split(',')[1];
                }

                return Convert.FromBase64String(base64String);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageHelper] Error en ConvertBase64ToByteArray: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region VALIDACIÓN DE IMÁGENES

        /// <summary>
        /// ⚡ OPTIMIZADO: Valida que el archivo sea una imagen válida con verificaciones de seguridad
        /// </summary>
        public static bool IsValidImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return false;

            try
            {
                // 1. Validar extensión del archivo
                string extension = Path.GetExtension(file.FileName)?.ToLower();
                if (string.IsNullOrEmpty(extension) || !ValidExtensions.Contains(extension))
                {
                    System.Diagnostics.Debug.WriteLine($"[ImageHelper] Extensión no válida: {extension}");
                    return false;
                }

                // 2. Validar tipo MIME
                if (string.IsNullOrEmpty(file.ContentType) ||
                    !ValidMimeTypes.Contains(file.ContentType.ToLower()))
                {
                    System.Diagnostics.Debug.WriteLine($"[ImageHelper] MIME type no válido: {file.ContentType}");
                    return false;
                }

                // 3. Validar que realmente sea una imagen intentando cargarla
                // Guardar posición actual del stream
                long originalPosition = -1;
                if (file.InputStream.CanSeek)
                {
                    originalPosition = file.InputStream.Position;
                    file.InputStream.Position = 0;
                }

                try
                {
                    using (var image = Image.FromStream(file.InputStream, true, true))
                    {
                        // Validaciones adicionales de seguridad
                        if (image.Width <= 0 || image.Height <= 0)
                            return false;

                        // Validar dimensiones máximas (evitar imágenes excesivamente grandes)
                        if (image.Width > 10000 || image.Height > 10000)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ImageHelper] Imagen demasiado grande: {image.Width}x{image.Height}");
                            return false;
                        }
                    }
                }
                finally
                {
                    // Restaurar posición original del stream
                    if (file.InputStream.CanSeek && originalPosition >= 0)
                    {
                        file.InputStream.Position = originalPosition;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageHelper] Error en validación de imagen: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Valida imagen desde byte array
        /// </summary>
        public static bool IsValidImageBytes(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return false;

            try
            {
                using (var ms = new MemoryStream(imageBytes))
                using (var image = Image.FromStream(ms))
                {
                    return image.Width > 0 && image.Height > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valida tamaño máximo de imagen
        /// </summary>
        public static bool IsFileSizeValid(HttpPostedFileBase file, int maxSizeMB)
        {
            if (file == null)
                return false;

            double fileSizeMB = GetFileSizeInMB(file);
            return fileSizeMB <= maxSizeMB && fileSizeMB > 0;
        }

        /// <summary>
        /// ⚡ NUEVO: Valida dimensiones mínimas de imagen
        /// </summary>
        public static bool HasValidDimensions(HttpPostedFileBase file, int minWidth = 100, int minHeight = 100)
        {
            if (file == null)
                return false;

            try
            {
                // Guardar posición del stream
                long originalPosition = -1;
                if (file.InputStream.CanSeek)
                {
                    originalPosition = file.InputStream.Position;
                    file.InputStream.Position = 0;
                }

                try
                {
                    using (var image = Image.FromStream(file.InputStream))
                    {
                        return image.Width >= minWidth && image.Height >= minHeight;
                    }
                }
                finally
                {
                    // Restaurar posición
                    if (file.InputStream.CanSeek && originalPosition >= 0)
                    {
                        file.InputStream.Position = originalPosition;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region REDIMENSIONAMIENTO Y OPTIMIZACIÓN

        /// <summary>
        /// ⚡ OPTIMIZADO: Redimensiona una imagen manteniendo la proporción con mejor calidad
        /// </summary>
        public static byte[] ResizeImage(byte[] imageBytes, int maxWidth, int maxHeight, int quality = 90)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            try
            {
                using (var ms = new MemoryStream(imageBytes))
                using (var sourceImage = Image.FromStream(ms))
                {
                    // Si la imagen ya es menor, no redimensionar
                    if (sourceImage.Width <= maxWidth && sourceImage.Height <= maxHeight)
                        return imageBytes;

                    // Calcular nuevas dimensiones manteniendo proporción
                    var (newWidth, newHeight) = CalculateNewDimensions(
                        sourceImage.Width,
                        sourceImage.Height,
                        maxWidth,
                        maxHeight
                    );

                    // Crear imagen redimensionada con alta calidad
                    using (var destImage = new Bitmap(newWidth, newHeight))
                    {
                        destImage.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);

                        using (var graphics = Graphics.FromImage(destImage))
                        {
                            // Configuración para máxima calidad
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = SmoothingMode.HighQuality;
                            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            // Dibujar imagen redimensionada
                            using (var wrapMode = new ImageAttributes())
                            {
                                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                                graphics.DrawImage(sourceImage,
                                    new Rectangle(0, 0, newWidth, newHeight),
                                    0, 0, sourceImage.Width, sourceImage.Height,
                                    GraphicsUnit.Pixel, wrapMode);
                            }
                        }

                        // Guardar con compresión de calidad especificada
                        return SaveImageWithQuality(destImage, quality);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageHelper] Error en ResizeImage: {ex.Message}");
                return imageBytes; // Retornar imagen original si falla
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Crea thumbnail cuadrado (crop center)
        /// </summary>
        public static byte[] CreateSquareThumbnail(byte[] imageBytes, int size)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            try
            {
                using (var ms = new MemoryStream(imageBytes))
                using (var sourceImage = Image.FromStream(ms))
                {
                    // Calcular área de recorte (centro)
                    int sourceSize = Math.Min(sourceImage.Width, sourceImage.Height);
                    int sourceX = (sourceImage.Width - sourceSize) / 2;
                    int sourceY = (sourceImage.Height - sourceSize) / 2;

                    using (var destImage = new Bitmap(size, size))
                    {
                        using (var graphics = Graphics.FromImage(destImage))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = SmoothingMode.HighQuality;

                            graphics.DrawImage(sourceImage,
                                new Rectangle(0, 0, size, size),
                                new Rectangle(sourceX, sourceY, sourceSize, sourceSize),
                                GraphicsUnit.Pixel);
                        }

                        return SaveImageWithQuality(destImage, 85);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageHelper] Error en CreateSquareThumbnail: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Optimiza imagen reduciendo calidad sin redimensionar
        /// </summary>
        public static byte[] OptimizeImage(byte[] imageBytes, int quality = 85)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            try
            {
                using (var ms = new MemoryStream(imageBytes))
                using (var image = Image.FromStream(ms))
                {
                    return SaveImageWithQuality(image, quality);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageHelper] Error en OptimizeImage: {ex.Message}");
                return imageBytes;
            }
        }

        #endregion

        #region INFORMACIÓN DE IMÁGENES

        /// <summary>
        /// Obtiene el tamaño del archivo en MB
        /// </summary>
        public static double GetFileSizeInMB(HttpPostedFileBase file)
        {
            if (file == null)
                return 0;

            return Math.Round((double)file.ContentLength / (1024 * 1024), 2);
        }

        /// <summary>
        /// ⚡ NUEVO: Obtiene dimensiones de la imagen
        /// </summary>
        public static (int width, int height) GetImageDimensions(HttpPostedFileBase file)
        {
            if (file == null)
                return (0, 0);

            try
            {
                // Guardar posición del stream
                long originalPosition = -1;
                if (file.InputStream.CanSeek)
                {
                    originalPosition = file.InputStream.Position;
                    file.InputStream.Position = 0;
                }

                try
                {
                    using (var image = Image.FromStream(file.InputStream))
                    {
                        return (image.Width, image.Height);
                    }
                }
                finally
                {
                    // Restaurar posición
                    if (file.InputStream.CanSeek && originalPosition >= 0)
                    {
                        file.InputStream.Position = originalPosition;
                    }
                }
            }
            catch
            {
                return (0, 0);
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Obtiene dimensiones desde byte array
        /// </summary>
        public static (int width, int height) GetImageDimensionsFromBytes(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return (0, 0);

            try
            {
                using (var ms = new MemoryStream(imageBytes))
                using (var image = Image.FromStream(ms))
                {
                    return (image.Width, image.Height);
                }
            }
            catch
            {
                return (0, 0);
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Obtiene información completa de la imagen
        /// </summary>
        public static ImageInfo GetImageInfo(HttpPostedFileBase file)
        {
            if (file == null)
                return null;

            try
            {
                var info = new ImageInfo
                {
                    FileName = Path.GetFileName(file.FileName),
                    Extension = Path.GetExtension(file.FileName)?.ToLower(),
                    ContentType = file.ContentType,
                    SizeInBytes = file.ContentLength,
                    SizeInMB = GetFileSizeInMB(file)
                };

                // Guardar posición del stream
                long originalPosition = -1;
                if (file.InputStream.CanSeek)
                {
                    originalPosition = file.InputStream.Position;
                    file.InputStream.Position = 0;
                }

                try
                {
                    using (var image = Image.FromStream(file.InputStream))
                    {
                        info.Width = image.Width;
                        info.Height = image.Height;
                        info.HorizontalResolution = image.HorizontalResolution;
                        info.VerticalResolution = image.VerticalResolution;
                        info.PixelFormat = image.PixelFormat.ToString();
                    }
                }
                finally
                {
                    // Restaurar posición
                    if (file.InputStream.CanSeek && originalPosition >= 0)
                    {
                        file.InputStream.Position = originalPosition;
                    }
                }

                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageHelper] Error en GetImageInfo: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region MÉTODOS AUXILIARES PRIVADOS

        /// <summary>
        /// Calcula nuevas dimensiones manteniendo la proporción
        /// </summary>
        private static (int width, int height) CalculateNewDimensions(
            int currentWidth,
            int currentHeight,
            int maxWidth,
            int maxHeight)
        {
            double ratioX = (double)maxWidth / currentWidth;
            double ratioY = (double)maxHeight / currentHeight;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(currentWidth * ratio);
            int newHeight = (int)(currentHeight * ratio);

            return (newWidth, newHeight);
        }

        /// <summary>
        /// Guarda imagen con calidad JPEG especificada
        /// </summary>
        private static byte[] SaveImageWithQuality(Image image, int quality)
        {
            // Validar rango de calidad
            quality = Math.Max(1, Math.Min(100, quality));

            using (var outputStream = new MemoryStream())
            {
                // Configurar encoder JPEG con calidad
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                var jpegCodec = GetEncoderInfo("image/jpeg");

                if (jpegCodec != null)
                {
                    image.Save(outputStream, jpegCodec, encoderParameters);
                }
                else
                {
                    // Fallback si no se encuentra el codec
                    image.Save(outputStream, ImageFormat.Jpeg);
                }

                return outputStream.ToArray();
            }
        }

        /// <summary>
        /// Obtiene el encoder para un MIME type específico
        /// </summary>
        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            var encoders = ImageCodecInfo.GetImageEncoders();
            return encoders.FirstOrDefault(e => e.MimeType == mimeType);
        }

        #endregion

        #region VALIDACIÓN DE FORMATO

        /// <summary>
        /// ⚡ NUEVO: Detecta formato de imagen desde byte array
        /// </summary>
        public static string DetectImageFormat(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length < 12)
                return "unknown";

            try
            {
                // JPEG: FF D8 FF
                if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF)
                    return "jpeg";

                // PNG: 89 50 4E 47 0D 0A 1A 0A
                if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 &&
                    imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                    return "png";

                // GIF: 47 49 46 38
                if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 &&
                    imageBytes[2] == 0x46 && imageBytes[3] == 0x38)
                    return "gif";

                return "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        #endregion
    }

    #region CLASES AUXILIARES

    /// <summary>
    /// ⚡ NUEVO: Clase para almacenar información de imagen
    /// </summary>
    public class ImageInfo
    {
        public string FileName { get; set; }
        public string Extension { get; set; }
        public string ContentType { get; set; }
        public int SizeInBytes { get; set; }
        public double SizeInMB { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float HorizontalResolution { get; set; }
        public float VerticalResolution { get; set; }
        public string PixelFormat { get; set; }

        public bool IsLandscape => Width > Height;
        public bool IsPortrait => Height > Width;
        public bool IsSquare => Width == Height;
        public double AspectRatio => Height > 0 ? (double)Width / Height : 0;
    }

    #endregion
}