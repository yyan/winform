﻿using System;
using System.Data;
using System.Data.Common;
using System.Web.Mvc;
using System.Collections.Generic;
using JCodes.Framework.BLL;
using JCodes.Framework.Entity;
using JCodes.Framework.Common.Databases;
using JCodes.Framework.Common;
using JCodes.Framework.jCodesenum.BaseEnum;
using JCodes.Framework.Common.Framework;

namespace JCodes.Framework.WebUI.Controllers
{
    /// <summary>
    /// 数据字典控制器
    /// </summary>
    public class DictDataController : BusinessController<DictData, DictDataInfo>
    {
        public DictDataController() : base()
        {
        }

        #region 导入Excel数据操作

        //导入或导出的字段列表   
        string columnString = "字典大类,字典名称,字典值,备注,排序";

        /// <summary>
        /// 检查Excel文件的字段是否包含了必须的字段
        /// </summary>
        /// <param name="guid">附件的GUID</param>
        /// <returns></returns>
        public ActionResult CheckExcelColumns(string guid)
        {
            CommonResult result = new CommonResult();

            try
            {
                DataTable dt = ConvertExcelFileToTable(guid);
                if (dt != null)
                {
                    //检查列表是否包含必须的字段
                    result.Success = DataTableHelper.ContainAllColumns(dt, columnString);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(LogLevel.LOG_LEVEL_CRIT, ex, typeof(DictDataController));
                result.ErrorMessage = ex.Message;
            }

            return ToJsonContent(result);
        }

        /// <summary>
        /// 获取服务器上的Excel文件，并把它转换为实体列表返回给客户端
        /// </summary>
        /// <param name="guid">附件的GUID</param>
        /// <returns></returns>
        public ActionResult GetExcelData(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            List<DictDataInfo> list = new List<DictDataInfo>();

            DataTable table = ConvertExcelFileToTable(guid);
            if (table != null)
            {
                #region 数据转换
                int i = 1;
                foreach (DataRow dr in table.Rows)
                {
                    bool converted = false;
                    DateTime dtDefault = Convert.ToDateTime("1900-01-01");
                    DateTime dt;

                    string typeName = dr["字典大类"].ToString();
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        DictTypeInfo typeInfo = BLLFactory<DictType>.Instance.FindSingle(string.Format("Name ='{0}'", typeName));
                        if (typeInfo != null)
                        {
                            DictDataInfo info = new DictDataInfo();
                            info.DictType_ID = typeInfo.ID;

                            info.Name = dr["字典名称"].ToString();
                            info.Value = Convert.ToInt32( dr["字典值"]);
                            info.Remark = dr["备注"].ToString();
                            info.Seq = dr["排序"].ToString();

                            info.Editor = CurrentUser.ID.ToString();
                            info.LastUpdated = DateTime.Now;

                            list.Add(info);
                        }
                    }
                }
                #endregion
            }

            var result = new { total = list.Count, rows = list };
            return ToJsonContentDate(result);
        }

        /// <summary>
        /// 保存客户端上传的相关数据列表
        /// </summary>
        /// <param name="list">数据列表</param>
        /// <returns></returns>
        public ActionResult SaveExcelData(List<DictDataInfo> list)
        {
            CommonResult result = new CommonResult();
            if (list != null && list.Count > 0)
            {
                #region 采用事务进行数据提交

                DbTransaction trans = BLLFactory<DictData>.Instance.CreateTransaction();
                if (trans != null)
                {
                    try
                    {
                        //int seq = 1;
                        foreach (DictDataInfo detail in list)
                        {
                            DictTypeInfo typeInfo = BLLFactory<DictType>.Instance.FindSingle(string.Format("Name ='{0}'", detail.DictType_ID));
                            if (typeInfo != null)
                            {
                                //detail.Seq = seq++;//增加1
                                detail.Editor = CurrentUser.ID.ToString();
                                detail.LastUpdated = DateTime.Now;

                                BLLFactory<DictData>.Instance.Insert(detail, trans);
                            }
                        }
                        trans.Commit();
                        result.Success = true;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLog(LogLevel.LOG_LEVEL_CRIT, ex, typeof(DictDataController));
                        result.ErrorMessage = ex.Message;
                        trans.Rollback();
                    }
                }
                #endregion
            }
            else
            {
                result.ErrorMessage = "导入信息不能为空";
            }

            return ToJsonContent(result);
        }

        /// <summary>
        /// 根据查询条件导出列表数据
        /// </summary>
        /// <returns></returns>
        public ActionResult Export()
        {
            #region 根据参数获取List列表
            string where = GetPagerCondition();
            string CustomedCondition = Request["CustomedCondition"] ?? "";
            List<DictDataInfo> list = new List<DictDataInfo>();

            if (!string.IsNullOrWhiteSpace(CustomedCondition))
            {
                //如果为自定义的json参数列表，那么可以使用字典反序列化获取参数，然后处理
                //Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(CustomedCondition);

                //如果是条件的自定义，可以使用Find查找
                list = baseBLL.Find(CustomedCondition);
            }
            else
            {
                list = baseBLL.Find(where);
            }

            #endregion

            #region 把列表转换为DataTable
            DataTable datatable = DataTableHelper.CreateTable("序号|int," + columnString);
            DataRow dr;
            int j = 1;
            for (int i = 0; i < list.Count; i++)
            {
                dr = datatable.NewRow();
                dr["序号"] = j++;
                DictTypeInfo typeInfo = BLLFactory<DictType>.Instance.FindByID(list[i].DictType_ID);
                if (typeInfo != null)
                {
                    dr["字典大类"] = typeInfo.Name;
                }
                dr["字典名称"] = list[i].Name;
                dr["字典值"] = list[i].Value;
                dr["备注"] = list[i].Remark;
                dr["排序"] = list[i].Seq;

                datatable.Rows.Add(dr);
            }
            #endregion

            #region 把DataTable转换为Excel并输出

            //根据用户创建目录，确保生成的文件不会产生冲突
            string filePath = string.Format("/GenerateFiles/{0}/DictData.xls", CurrentUser.Name);
            GenerateExcel(datatable, filePath);

            #endregion

            //返回生成后的文件路径，让客户端根据地址下载
            return Content(filePath);
        }

        #endregion


        #region 写入数据前修改部分属性
        protected override void OnBeforeInsert(DictDataInfo info)
        {
            //留给子类对参数对象进行修改
            info.Editor = CurrentUser.ID.ToString();
            info.LastUpdated = DateTime.Now;
        }

        protected override void OnBeforeUpdate(DictDataInfo info)
        {
            //留给子类对参数对象进行修改
            info.Editor = CurrentUser.ID.ToString();
            info.LastUpdated = DateTime.Now;
        }
        #endregion

        public override ActionResult FindWithPager()
        {
            //检查用户是否有权限，否则抛出MyDenyAccessException异常
            base.CheckAuthorized(AuthorizeKey.ListKey);

            string where = GetPagerCondition();
            PagerInfo pagerInfo = GetPagerInfo();
            List<DictDataInfo> list = baseBLL.FindWithPager(where, pagerInfo);

            //如果需要修改字段显示，则参考下面代码处理
            //foreach(DictDataInfo info in list)
            //{
            //    info.PID = BLLFactory<DictData>.Instance.GetFieldValue(info.PID, "Name");
            //    if (!string.IsNullOrEmpty(info.Creator))
            //    {
            //        info.Creator = BLLFactory<User>.Instance.GetFullNameByID(info.Creator.ToInt32());
            //    }
            //}

            //Json格式的要求{total:22,rows:{}}
            //构造成Json的格式传递
            var result = new { total = pagerInfo.RecordCount, rows = list };
            return ToJsonContentDate(result);
        }

        /// <summary>
        /// 批量添加字典数据操作
        /// </summary>
        /// <param name="DictType_ID">字典类型</param>
        /// <param name="Seq">排序开始或前缀</param>
        /// <param name="Data">批量插入的内容</param>
        /// <param name="SplitType">分开类型，分隔符分开（Split）还是行分割（Line）</param>
        /// <param name="Remark">备注</param>
        /// <returns></returns>
        public ActionResult BatchInsert(string DictType_ID, string Seq, string Data, string SplitType, string Remark)
        {
            CommonResult result = new CommonResult();
            if (string.IsNullOrEmpty(DictType_ID) || string.IsNullOrEmpty(Data))
            {
                result.ErrorMessage = "DictType_ID或Data参数为空";
                return ToJsonContent(result);
            }

            string[] arrayItems = Data.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            int intSeq = -1;
            int seqLength = 3;
            string strSeq = Seq;
            if (int.TryParse(strSeq, out intSeq))
            {
                seqLength = strSeq.Length;
            }

            if (arrayItems != null && arrayItems.Length > 0)
            {
                DbTransaction trans = BLLFactory<DictData>.Instance.CreateTransaction();
                if (trans != null)
                {
                    try
                    {
                        #region MyRegion
                        foreach (string strItem in arrayItems)
                        {
                            if (SplitType.Equals("split", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrWhiteSpace(strItem))
                                {
                                    string[] dataItems = strItem.Split(new char[] { ',', '，', ';', '；', '/', '、' });
                                    foreach (string dictData in dataItems)
                                    {
                                        #region 保存数据
                                        string seq = "";
                                        if (intSeq > 0)
                                        {
                                            seq = (intSeq++).ToString().PadLeft(seqLength, '0');
                                        }
                                        else
                                        {
                                            seq = string.Format("{0}{1}", strSeq, intSeq++);
                                        }

                                        InsertDictData(DictType_ID, dictData, seq, Remark, trans);
                                        #endregion
                                    }
                                }
                            }
                            else
                            {
                                #region 保存数据
                                if (!string.IsNullOrWhiteSpace(strItem))
                                {
                                    string seq = "";
                                    if (intSeq > 0)
                                    {
                                        seq = (intSeq++).ToString().PadLeft(seqLength, '0');
                                    }
                                    else
                                    {
                                        seq = string.Format("{0}{1}", strSeq, intSeq++);
                                    }

                                    InsertDictData(DictType_ID, strItem, seq, Remark, trans);
                                }
                                #endregion
                            }
                        }
                        #endregion

                        trans.Commit();
                        result.Success = true;
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        LogHelper.WriteLog(LogLevel.LOG_LEVEL_CRIT, ex, typeof(DictDataController));
                        result.ErrorMessage = ex.Message;
                    }
                }
            }

            return ToJsonContent(result);
        }

        /// <summary>
        /// 使用事务参数，插入数据，最后统一提交事务处理
        /// </summary>
        /// <param name="dictData">字典数据</param>
        /// <param name="seq">排序</param>
        /// <param name="trans">事务对象</param>
        private void InsertDictData(string dictTypeId, string dictData, string seq, string note, DbTransaction trans)
        {
            if (!string.IsNullOrWhiteSpace(dictData))
            {
                DictDataInfo info = new DictDataInfo();
                info.Editor = CurrentUser.ID.ToString();
                info.LastUpdated = DateTime.Now;

                info.DictType_ID = Convert.ToInt32( dictTypeId);
                info.Name = dictData.Trim();
                info.Value = Convert.ToInt32(dictData.Trim());
                info.Remark = note;
                info.Seq = seq;

                bool succeed = BLLFactory<DictData>.Instance.Insert(info, trans);
            }
        }
    }
}
