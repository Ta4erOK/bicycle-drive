using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace BicycleDrive
{
    /// <summary>
    /// Работа с файлами изображений товаров.
    ///
    /// В базе данных хранятся ТОЛЬКО относительные имена файлов (например «1.jpg»),
    /// сами файлы лежат в папке Images рядом с исполняемым файлом приложения.
    /// Класс решает три задачи:
    ///   1) показать картинку по относительному пути, не блокируя файл на диске;
    ///   2) скопировать выбранный пользователем файл в папку Images (с защитой от
    ///      ошибок прав доступа и занятости файла);
    ///   3) удалить файл изображения, когда товар удалён или картинку заменили.
    /// </summary>
    public static class ImageStorage
    {
        /// <summary>Заглушка для товаров без фотографии.</summary>
        public const string Placeholder = "picture.png";

        /// <summary>Папка с изображениями рядом с exe.</summary>
        public static string ImagesDirectory
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images"); }
        }

        /// <summary>
        /// Полный путь к файлу изображения по относительному имени.
        /// Если файла нет — возвращает путь к заглушке picture.png.
        /// </summary>
        public static string ResolvePath(string relativePath)
        {
            string name = string.IsNullOrWhiteSpace(relativePath)
                ? Placeholder
                : Path.GetFileName(relativePath.Trim());

            string fullPath = Path.Combine(ImagesDirectory, name);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            string placeholderPath = Path.Combine(ImagesDirectory, Placeholder);
            return File.Exists(placeholderPath) ? placeholderPath : null;
        }

        /// <summary>
        /// Загружает изображение по относительному пути БЕЗ кэширования.
        ///
        /// Ключевой момент: файл открывается через FileStream, данные читаются
        /// в память (BitmapCacheOption.OnLoad), а поток закрывается в using.
        /// Если поток не закрыть, Windows считает файл занятым и его нельзя
        /// ни перезаписать, ни удалить — именно поэтому кэш и UriSource здесь
        /// не используются.
        /// </summary>
        public static BitmapImage GetImageWithoutCaching(string relativePath)
        {
            string fullPath = ResolvePath(relativePath);
            if (fullPath == null)
            {
                return null;
            }

            try
            {
                using (var stream = new FileStream(fullPath, FileMode.Open,
                                                   FileAccess.Read, FileShare.Read))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;      // прочитать сразу
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();                                    // поток ещё открыт
                    bitmap.Freeze();                                     // можно отдавать в UI-поток
                    return bitmap;
                }                                                        // поток закрыт здесь
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Загружает изображение с произвольного абсолютного пути (например,
        /// только что выбранного пользователем файла ещё вне папки приложения).
        /// </summary>
        public static BitmapImage GetImageFromFile(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                return GetImageWithoutCaching(Placeholder);
            }

            try
            {
                using (var stream = new FileStream(fullPath, FileMode.Open,
                                                   FileAccess.Read, FileShare.ReadWrite))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch (Exception)
            {
                return GetImageWithoutCaching(Placeholder);
            }
        }

        /// <summary>
        /// Копирует выбранный файл в папку Images и возвращает относительное имя,
        /// которое записывается в БД. Если файл с таким именем уже есть (другой
        /// файл), к имени добавляется числовой суффикс.
        ///
        /// Ошибки прав доступа и занятости файла перехватываются и возвращаются
        /// текстом в параметре error — вызывающий код показывает их пользователю,
        /// а приложение не падает.
        /// </summary>
        public static string SaveImage(FileInfo selectedImage, out string error)
        {
            error = null;

            if (selectedImage == null || !selectedImage.Exists)
            {
                error = "Выбранный файл изображения не найден.";
                return null;
            }

            try
            {
                Directory.CreateDirectory(ImagesDirectory);

                string fileName = selectedImage.Name;
                string targetPath = Path.Combine(ImagesDirectory, fileName);

                // тот же самый файл (уже лежит в папке Images) — копировать не нужно
                if (string.Equals(Path.GetFullPath(selectedImage.FullName),
                                  Path.GetFullPath(targetPath),
                                  StringComparison.OrdinalIgnoreCase))
                {
                    return fileName;
                }

                // имя занято другим файлом — подбираем свободное
                if (File.Exists(targetPath))
                {
                    fileName = MakeUniqueName(fileName);
                    targetPath = Path.Combine(ImagesDirectory, fileName);
                }

                selectedImage.CopyTo(targetPath, true);
                return fileName;
            }
            catch (UnauthorizedAccessException)
            {
                error = "Нет прав на запись в папку с изображениями. "
                      + "Запустите приложение от имени администратора или выберите другую папку.";
                return null;
            }
            catch (IOException ex)
            {
                error = "Не удалось скопировать изображение: " + ex.Message
                      + " Проверьте, не открыт ли файл в другой программе.";
                return null;
            }
            catch (Exception ex)
            {
                error = "Не удалось сохранить изображение: " + ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Удаляет файл изображения из папки Images.
        /// Заглушка picture.png не удаляется никогда.
        /// Возвращает true, если файла больше нет (удалён или отсутствовал).
        /// </summary>
        public static bool DeleteImage(string relativePath, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return true;
            }

            string fileName = Path.GetFileName(relativePath.Trim());
            if (string.Equals(fileName, Placeholder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string fullPath = Path.Combine(ImagesDirectory, fileName);
            if (!File.Exists(fullPath))
            {
                return true;
            }

            try
            {
                File.Delete(fullPath);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                error = "Нет прав на удаление файла «" + fileName + "».";
                return false;
            }
            catch (IOException ex)
            {
                error = "Не удалось удалить файл «" + fileName + "»: " + ex.Message;
                return false;
            }
        }

        /// <summary>Подбирает свободное имя файла: 2.jpg → 2_1.jpg → 2_2.jpg …</summary>
        private static string MakeUniqueName(string fileName)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            for (int index = 1; index < 1000; index++)
            {
                string candidate = baseName + "_" + index + extension;
                if (!File.Exists(Path.Combine(ImagesDirectory, candidate)))
                {
                    return candidate;
                }
            }

            return Guid.NewGuid().ToString("N") + extension;
        }
    }
}
