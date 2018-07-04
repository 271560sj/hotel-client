using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.IO;

namespace HotelUpdateService.update.utils
{
    /// <summary>
    /// 文件压缩与解压缩相关工具
    /// </summary>
    class ZipHelper
    {
        /// <summary>
        /// 打包压缩文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <param name="stream"></param>
        /// <param name="staticPath"></param>
        /// <returns></returns>
        #region public static bool zipDirectory(String path, ZipOutputStream stream, String staticPath)
        public static bool zipDirectory(String path, ZipOutputStream stream, String staticPath)
        {
            //记录压缩结果
            bool result = false;
            Crc32 crc = new Crc32();
            try
            {
                //获取文件加下的一级目录文件
                String[] files = Directory.GetFileSystemEntries(path);
                if (files.Length <= 0)//判断没有文件，返回成功
                {
                    Logger.info(typeof(ZipHelper), String.Format(@"no file in directory {0}", path));
                    result = true;
                }
                foreach (String file in files)//遍历文件
                {
                    if (Directory.Exists(file))//是文件夹
                    {
                        //todo ,压缩时，忽略对更新程序的文件夹的压缩（当更新程序和主程序在同一目录时）
                        if (file.Equals(String.Format(@"{0}\Debug", path)))
                        {
                            continue;
                        }
                        //递归压缩文件夹
                        result = zipDirectory(file, stream, staticPath);
                        if (!result)//文件夹压缩失败，结果循环
                        {
                            break;
                        }
                    }
                    else//压缩文件
                    {
                        //获取文件流
                        FileStream fileStream = File.OpenRead(file);
                        //用于读取文件流
                        byte[] buffer = new byte[fileStream.Length];
                        fileStream.Read(buffer, 0, buffer.Length);
                        //将文件保存到临时文件中
                        String tempFile = file.Substring(staticPath.LastIndexOf(@"\") + 1);
                        ZipEntry entry = new ZipEntry(tempFile);
                        //设置文件属性信息
                        entry.DateTime = DateTime.Now;
                        entry.Size = fileStream.Length;
                        fileStream.Close();//关闭文件流
                        crc.Reset();//重置CRC校验信息
                        crc.Update(buffer);//更新字节数组
                        entry.Crc = crc.Value;//设置CRC信息
                        stream.PutNextEntry(entry);//压缩到文件中
                        stream.Write(buffer, 0, buffer.Length);//写入压缩文件
                        result = true;
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            catch (IOException ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            catch (Exception ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            return result;
        }
        #endregion

        /// <summary>
        /// 解压缩zip文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        #region public static bool unZipFile(String path)
        public static bool unZipFile(String path)
        {
            //标记操作结果
            bool result = false;

            if (!File.Exists(path))//目录不存在，返回操作失败信息
            {
                Logger.info(typeof(ZipHelper), "file not exists.");
                return result;
            }
            //开始解压
            try
            {
                //获取文件流
                using (ZipInputStream zis = new ZipInputStream(File.OpenRead(path)))
                {
                    ZipEntry entry;
                    //循环遍历文件流中的entry
                    while ((entry = zis.GetNextEntry()) != null)
                    {
                        Logger.info(typeof(ZipHelper), String.Format("unzip {0}", entry.Name));
                        //获取文件的目录
                        string directoryName = Path.GetDirectoryName(entry.Name);
                        //获取文件名称
                        string fileName = Path.GetFileName(entry.Name);

                        if (directoryName.Length > 0)//目录存在，则创建对应的目录
                        {
                            Directory.CreateDirectory(String.Format(@"back\{0}", directoryName));
                        }

                        if (!String.IsNullOrEmpty(fileName))//文件存在
                        {
                            //创建文件流
                            using (FileStream fis = File.Create(String.Format(@"back\{0}", entry.Name)))
                            {
                                //读取文件流
                                byte[] data = new byte[1024 * 10];
                                while (true)
                                {
                                    int size = zis.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        fis.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                result = true;
            }
            catch (IOException ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            catch (NotSupportedException ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            catch (Exception ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            return result;
        }
        #endregion

        /// <summary>
        /// 解压缩tar文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        #region public static bool unTarFile(String path, String directory)
        public static bool unTarFile(String path, String directory)
        {
            //标记结果
            bool result = false;
            //判断传入参数是否合法
            if(String.IsNullOrEmpty(path) || String.IsNullOrEmpty(directory))
            {
                Logger.info(typeof(ZipHelper), "tar file path is empty.");
                return result;
            }
            //判断文件是否存在
            if (!File.Exists(path))
            {
                Logger.info(typeof(ZipHelper), "tar file not exist.");
                return result;
            }
            //开始解压
            try
            {
                //获取文件流
                using (TarInputStream tis = new TarInputStream(File.OpenRead(path)))
                {
                    //记录文件流实体
                    TarEntry entry = null;
                    //循环遍历文件流
                    while((entry = tis.GetNextEntry()) != null)
                    {
                        Logger.info(typeof(ZipHelper), String.Format("untar {0}", entry.Name));
                        //获取文件目录
                        String parent = Path.GetDirectoryName(entry.Name);
                        //获取文件名称
                        String name = Path.GetFileName(entry.Name);
                        if(parent.Length > 0)//判断目录存在，在本地文件系统创建对应文件夹
                        {
                            Directory.CreateDirectory(String.Format(@"{0}\{1}", directory, parent));
                        }
                        if (!String.IsNullOrEmpty(name))//判断文件存在
                        {
                            //创建文件
                            using (FileStream fis = File.Create(String.Format(@"{0}\{1}", directory,entry.Name)))
                            {
                                //写入文件
                                byte[] data = new byte[1024 * 10];
                                while (true)
                                {
                                    int size = tis.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        fis.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                result = true;
            }
            catch(Exception ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            return result;
            
        }
        #endregion
    }
}
