using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraSplashScreen;
using DevExpress.XtraNavBar;
using JCodes.Framework.CommonControl.BaseUI;
using JCodes.Framework.CommonControl.Controls;
using JCodes.Framework.Common.Files;
using JCodes.Framework.Entity;
using System.Xml;
using DevExpress.XtraTab;
using DevExpress.XtraEditors;
using DevExpress.XtraRichEdit.API.Native;
using System;
using DevExpress.Office.Utils;
using DevExpress.XtraRichEdit.Services;
using System.Drawing;
using DevExpress.Utils;
using DevExpress.XtraRichEdit.Commands;
using DevExpress.XtraRichEdit;
using DevExpress.Office.Internal;
using DevExpress.XtraRichEdit.Export;
using DevExpress.XtraRichEdit.Import;
using DevExpress.XtraRichEdit.Internal;
using JCodes.Framework.CommonControl.Other;
using JCodes.Framework.Common.Format;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using System.Text;

namespace JCodes.Framework.AddIn.Proj
{
    public partial class FrmTables : BaseDock
    {

        private XmlHelper xmltableshelper = new XmlHelper(@"XML\tables.xml");
        private XmlHelper xmltablesinfohelper = null;

        private List<DictInfo> dictTypeInfoList = null;

        private NavBarControl navBar = null;

        NavBarGroup selectedGroup = null;
        NavBarItemLink selectedLink = null;

        private string strBasicInfoGuid = string.Empty;
        DateEdit _txtlastupdate = null;

        DevExpress.XtraGrid.Views.Grid.GridView gridViewFields = null;
        DevExpress.XtraGrid.Views.Grid.GridView gridViewIndexs = null;

        private string xmlfieldsinfomodel = "<name>{0}</name><isnull>{1}</isnull><remark>{2}</remark>";

        private string xmlindexsinfomodel = "<name>{0}</name><indexfieldlst>{1}</indexfieldlst><unique>{2}</unique><index>{3}</index><primary>{4}</primary>";

        TableFieldsInfo tmptableFieldsInfo = null;
        TableIndexsInfo tmptableIndexsInfo = null;

        // 保存Sql预览控件
        DevExpress.XtraRichEdit.RichEditControl richEditControl = null;

        public FrmTables()
        {
            InitializeComponent();
        }

        void frmMain_Load(object sender, System.EventArgs e) {
            LoadDicData();

            BeginInvoke(new MethodInvoker(InitDemo));
        }
       
        void InitDemo() {
            // 创建一个NarBar;
            navBar = new NavBarControl();
            ((System.ComponentModel.ISupportInitialize)(navBar)).BeginInit();
            navBar.Dock = System.Windows.Forms.DockStyle.Fill;
            navBar.DragDropFlags = ((DevExpress.XtraNavBar.NavBarDragDrop)(((DevExpress.XtraNavBar.NavBarDragDrop.AllowDrag | DevExpress.XtraNavBar.NavBarDragDrop.AllowDrop)
            | DevExpress.XtraNavBar.NavBarDragDrop.AllowOuterDrop)));
            navBar.Location = new System.Drawing.Point(0, 0);
            navBar.Margin = new System.Windows.Forms.Padding(2);
            navBar.Name = "navBar";
            navBar.OptionsNavPane.ExpandedWidth = 139;
            navBar.Size = new System.Drawing.Size(139, 373);
            navBar.StoreDefaultPaintStyleName = true;
            navBar.TabIndex = 3;
            navBar.Text = "navBar";

            navBar.MouseDown += (sender, e) =>
            {
                if (e.Button != MouseButtons.Right) return;
                var navBarControl1 = sender as NavBarControl;
                NavBarHitInfo hi = navBarControl1.CalcHitInfo(new Point(e.X, e.Y));
                selectedGroup = hi.Group;
                if (hi.InLink)
                {
                    selectedLink = hi.Link;
                    pmItem.ShowPopup(navBarControl1.PointToScreen(new Point(e.X, e.Y)));
                    return;
                }
                pmGroup.ShowPopup(navBarControl1.PointToScreen(new Point(e.X, e.Y)));
            };
            ucToolbox1.Controls.Add(navBar);
            ((System.ComponentModel.ISupportInitialize)(navBar)).EndInit();

            // 根据配置读取分类
            XmlNodeList xmlNodeLst = xmltableshelper.Read("datatype/tabletype");
            List<TablesTypeInfo> tablesTypeInfoList = new List<TablesTypeInfo>();
            foreach (XmlNode xn1 in xmlNodeLst)
            {
                TablesTypeInfo tablesTypeInfo = new TablesTypeInfo();
                // 将节点转换为元素，便于得到节点的属性值
                XmlElement xe = (XmlElement)xn1;
                // 得到Type和ISBN两个属性的属性值
                tablesTypeInfo.GUID = xe.GetAttribute("guid").ToString();
                tablesTypeInfo.CreateDate = xe.GetAttribute("createdate").ToString();
                tablesTypeInfo.Name = xe.GetAttribute("name").ToString();

                tablesTypeInfoList.Add(tablesTypeInfo);
            }

            XmlNodeList xmlNodeLst2 = xmltableshelper.Read("datatype/dataitem");
            List<TablesInfo> tablesInfoList = new List<TablesInfo>();
            foreach (XmlNode xn1 in xmlNodeLst2)
            {
                TablesInfo tablesInfo = new TablesInfo();
                // 将节点转换为元素，便于得到节点的属性值
                XmlElement xe = (XmlElement)xn1;
                // 得到Type和ISBN两个属性的属性值
                tablesInfo.GUID = xe.GetAttribute("guid").ToString();

                // 得到ConstantInfo节点的所有子节点
                XmlNodeList xnl0 = xe.ChildNodes;
                tablesInfo.Name = xnl0.Item(0).InnerText;
                tablesInfo.ChineseName = xnl0.Item(1).InnerText;
                tablesInfo.FunctionId = xnl0.Item(2).InnerText;
                tablesInfo.TypeGuid = xnl0.Item(3).InnerText;
                tablesInfo.Path = xnl0.Item(4).InnerText;

                tablesInfoList.Add(tablesInfo);
            }

            if (tablesTypeInfoList.Count == 0) return;

            foreach (var tablesTypeInfo in tablesTypeInfoList)
            {
                NavBarGroup standardGroup = navBar.Groups.Add();
                standardGroup.Caption = tablesTypeInfo.Name;
                standardGroup.Tag = tablesTypeInfo.GUID;
                standardGroup.Expanded = true;

                foreach (var tablesInfo in tablesInfoList)
                {
                    if (string.Equals(tablesTypeInfo.GUID, tablesInfo.TypeGuid))
                    {
                        NavBarItem item = new NavBarItem();
                        item.Caption = string.Format("{0}-({1} {2})", tablesInfo.FunctionId, tablesInfo.ChineseName, tablesInfo.Name);
                        // 临时调整为表名
                        item.Tag = tablesInfo.GUID;
                        item.Name = tablesInfo.Name;
                        item.Hint = tablesInfo.ChineseName;
                        item.LinkClicked += Item_LinkClicked;
                        navBar.Items.Add(item);
                        standardGroup.ItemLinks.Add(item);
                    }
                }
            }
        }

        void richEditControl_InitializeDocument(object sender, EventArgs e)
        {
            Document document = (sender as DevExpress.XtraRichEdit.RichEditControl).Document;
            document.BeginUpdate();
            document.DefaultCharacterProperties.FontName = "Courier New";
            document.DefaultCharacterProperties.FontSize = 10;
            document.Sections[0].Page.Width = Units.InchesToDocumentsF(50);
            document.Sections[0].LineNumbering.CountBy = 1;
            document.Sections[0].LineNumbering.RestartType = LineNumberingRestart.Continuous;
            document.EndUpdate();
        }

        /// <summary>
        /// 新增组
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bbiAddGroup_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            FrmEditGroupName dlg = new FrmEditGroupName();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                NavBarGroup standardGroup = navBar.Groups.Add();
                standardGroup.Tag = dlg.ID;
                standardGroup.Caption = dlg.strGroupName;
            }
        }

        /// <summary>
        /// 修改组
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bbiModGroup_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (selectedGroup == null)
            {
                MessageDxUtil.ShowError("请选择需要修改的分组");
                return;
            }

            FrmEditGroupName dlg = new FrmEditGroupName();
            dlg.ID = selectedGroup.Tag.ToString();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                selectedGroup.Caption = dlg.strGroupName;
            }

            selectedGroup = null;
        }

        /// <summary>
        /// 删除组
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void bbiDelGroup_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (selectedGroup == null)
            {
                MessageDxUtil.ShowError("请选择需要删除的分组");
                return;
            }
            // 需要重新读一下xml文件不存缓存里面没有
            xmltableshelper = new XmlHelper(@"XML\tables.xml");
            // 删除大的分类
            xmltableshelper.DeleteByPathNode(string.Format("datatype/tabletype/item[@guid=\"{0}\"]", selectedGroup.Tag));
            // 删除子项
            while (true)
            {
                try
                {
                    // 再删除子节点本身
                    var objXmlDoc = xmltableshelper.GetXmlDoc();
                    XmlNode xn = objXmlDoc.SelectSingleNode(string.Format("datatype/dataitem/item[typeguid=\"{0}\"]", selectedGroup.Tag));
                    // 删除table文件
                    if (FileUtil.FileIsExist(string.Format(@"XML\{0}.table", xn.FirstChild.InnerText)))
                    {
                        FileUtil.DeleteFile(string.Format(@"XML\{0}.table", xn.FirstChild.InnerText));
                    }

                    xn.ParentNode.RemoveChild(xn);
                }
                catch
                {
                    break;
                }
            }

            navBar.Groups.Remove(selectedGroup);

            xmltableshelper.Save(false);

            selectedGroup = null;
        }

        /// <summary>
        /// 新增项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bbiAddItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (selectedGroup == null)
            {
                MessageDxUtil.ShowError("请选择需要新增的分组");
                return;
            }

            FrmEditItemName dlg = new FrmEditItemName();

            dlg.strGuid = selectedGroup.Tag.ToString();

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                NavBarItem item = new NavBarItem();
                item.Caption = string.Format("{0}-({1} {2})", dlg.strFunction, dlg.strChineseName, dlg.strItemName);
                item.Tag = dlg.ID;
                item.Name = dlg.strItemName;
                item.Hint = dlg.strChineseName;
                item.LinkClicked += Item_LinkClicked;

                navBar.Items.Add(item);
                selectedGroup.ItemLinks.Add(item);
            }

            selectedGroup = null;
        }

        /// <summary>
        /// 修改项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bbiModItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (selectedLink == null)
            {
                MessageDxUtil.ShowError("请选择需要修改的项目");
                return;
            }

            FrmEditItemName dlg = new FrmEditItemName();
            dlg.ID = selectedLink.Item.Tag.ToString();
            dlg.strGuid = selectedGroup.Tag.ToString();

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                NavBarItem item = selectedLink.Item;
                item.Caption = string.Format("{0}-({1} {2})", dlg.strFunction, dlg.strChineseName, dlg.strItemName);
                item.Tag = dlg.ID;
                item.Name = dlg.strItemName;
                item.Hint = dlg.strChineseName;
            }

            selectedLink = null;
        }

        /// <summary>
        /// 删除项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void bbiDelItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (selectedLink == null)
            {
                MessageDxUtil.ShowError("请选择需要删除的项目");
                return;
            }

            xmltableshelper = new XmlHelper(@"XML\tables.xml");
            // 删除子项
            xmltableshelper.DeleteByPathNode(string.Format("datatype/dataitem/item[@guid=\"{0}\"]", selectedLink.Item.Tag));
            xmltableshelper.Save(false);

            // 删除table文件
            if (FileUtil.IsExistFile(string.Format(@"XML\{0}.table", selectedLink.Item.Name)))
            {
                FileUtil.DeleteFile(string.Format(@"XML\{0}.table", selectedLink.Item.Name));
            }

            // 界面删除元素
            selectedGroup.ItemLinks.Remove(selectedLink);

            selectedLink = null;
        }

        /// <summary>
        /// 点击表信息得到的结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_LinkClicked(object sender, NavBarLinkEventArgs e)
        {
            var item = sender as NavBarItem;

            // 判断是否已经存在了如果存在了则改为选中
            for (System.Int32 i = 0; i < tabbedView.Documents.Count; i++)
            {
                // 找到 选中
                if (string.Equals(tabbedView.Documents[i].Tag, item.Hint))
                {
                    tabbedView.Controller.Activate(tabbedView.Documents[i]);
                    dockPanel6.HideSliding();
                    return;
                }
            }

            tabbedView.BeginUpdate();
            UserControl control = new UserControl();

            XtraTabControl xtraTabControl1 = new XtraTabControl(); ;
            XtraTabPage xtraTabPageBasic = new XtraTabPage();
            XtraTabPage xtraTabPageFields = new XtraTabPage();
            XtraTabPage xtraTabPageSQLLook = new XtraTabPage();
            XtraTabPage xtraTabPageHistoryRecord = new XtraTabPage();

            ((System.ComponentModel.ISupportInitialize)(xtraTabControl1)).BeginInit();
            xtraTabControl1.SuspendLayout();

            xtraTabControl1.Dock = DockStyle.Fill;
            xtraTabControl1.HeaderLocation = DevExpress.XtraTab.TabHeaderLocation.Bottom;
            xtraTabControl1.Location = new System.Drawing.Point(0, 0);
            xtraTabControl1.Name = "xtraTabControl1";
            xtraTabControl1.SelectedTabPage = xtraTabPageBasic;
            xtraTabControl1.Size = new System.Drawing.Size(1206, 556);
            xtraTabControl1.TabIndex = 0;
            xtraTabControl1.TabPages.AddRange(new XtraTabPage[] { xtraTabPageBasic, xtraTabPageFields, xtraTabPageSQLLook, xtraTabPageHistoryRecord });

            #region 基本信息
            xtraTabPageBasic.Name = "xtraTabPageBasic";
            xtraTabPageBasic.Text = "基本信息";

            GroupControl groupControl1 = new GroupControl();
            Label lblobjectId = new Label();
            Label lblenglishName = new Label();
            Label lblchineseName = new Label();
            Label lblexistHisTable = new Label();
            Label lblDB = new Label();
            Label lblversion = new Label();
            Label lbllastupdate = new Label();
            Label lblremark = new Label();

            TextEdit txtobjectId = new TextEdit();
            TextEdit txtenglishName = new TextEdit();
            TextEdit txtchineseName = new TextEdit();
            TextEdit txtversion = new TextEdit();
            DateEdit txtlastupdate = new DateEdit();
            CheckEdit ckexistHisTable = new CheckEdit();
            ComboBoxEdit cbbDB = new ComboBoxEdit();
            MemoEdit meremark = new MemoEdit();

            ((System.ComponentModel.ISupportInitialize)(groupControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(txtobjectId.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(txtenglishName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(txtchineseName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(txtversion.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(txtlastupdate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(ckexistHisTable.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(cbbDB.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(meremark.Properties)).BeginInit();

            groupControl1.SuspendLayout();
            groupControl1.Dock = DockStyle.Fill;

            lblobjectId.Location = new System.Drawing.Point(5, 30);
            lblobjectId.Name = "lblobjectId";
            lblobjectId.Size = new System.Drawing.Size(90, 22);
            lblobjectId.Text = "对象号";
            lblobjectId.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            txtobjectId.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left));
            txtobjectId.Location = new System.Drawing.Point(100, 32);
            txtobjectId.Name = "txtobjectId";
            txtobjectId.Size = new System.Drawing.Size(180, 22);

            lblenglishName.Location = new System.Drawing.Point(5, 55);
            lblenglishName.Name = "lblenglishName";
            lblenglishName.Size = new System.Drawing.Size(90, 22);
            lblenglishName.Text = "英文名";
            lblenglishName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            txtenglishName.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left));
            txtenglishName.Location = new System.Drawing.Point(100, 57);
            txtenglishName.Name = "txtenglishName";
            txtenglishName.Size = new System.Drawing.Size(180, 22);

            lblchineseName.Location = new System.Drawing.Point(5, 80);
            lblchineseName.Name = "lblenglishName";
            lblchineseName.Size = new System.Drawing.Size(90, 22);
            lblchineseName.Text = "中文名";
            lblchineseName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            txtchineseName.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left));
            txtchineseName.Location = new System.Drawing.Point(100, 82);
            txtchineseName.Name = "txtchineseName";
            txtchineseName.Size = new System.Drawing.Size(180, 22);

            lblexistHisTable.Location = new System.Drawing.Point(5, 105);
            lblexistHisTable.Name = "lblexistHisTable";
            lblexistHisTable.Size = new System.Drawing.Size(90, 22);
            lblexistHisTable.Text = "存在历史表";
            lblexistHisTable.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            ckexistHisTable.Location = new System.Drawing.Point(100, 107);
            ckexistHisTable.Name = "ckexistHisTable";
            ckexistHisTable.Properties.Caption = "是";

            lblDB.Location = new System.Drawing.Point(5, 130);
            lblDB.Name = "lblDB";
            lblDB.Size = new System.Drawing.Size(90, 22);
            lblDB.Text = "数据库";
            lblDB.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            cbbDB.Location = new System.Drawing.Point(100, 132);
            cbbDB.Name = "cbbDB";
            cbbDB.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            cbbDB.Size = new System.Drawing.Size(180, 22);
            cbbDB.SelectedValueChanged += cbbDB_SelectedValueChanged;

            lblversion.Location = new System.Drawing.Point(5, 155);
            lblversion.Name = "lblversion";
            lblversion.Size = new System.Drawing.Size(90, 22);
            lblversion.Text = "版本号";
            lblversion.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            txtversion.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left));
            txtversion.Location = new System.Drawing.Point(100, 157);
            txtversion.Name = "txtversion";
            txtversion.Size = new System.Drawing.Size(180, 22);

            lbllastupdate.Location = new System.Drawing.Point(5, 180);
            lbllastupdate.Name = "lbllastupdate";
            lbllastupdate.Size = new System.Drawing.Size(90, 22);
            lbllastupdate.Text = "修改日期";
            lbllastupdate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            txtlastupdate.EditValue = null;
            txtlastupdate.Location = new System.Drawing.Point(100, 182);
            txtlastupdate.Name = "txtlastupdate";
            txtlastupdate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            txtlastupdate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            txtlastupdate.Size = new System.Drawing.Size(180, 22);

            lblremark.Location = new System.Drawing.Point(5, 205);
            lblremark.Name = "lblremark";
            lblremark.Size = new System.Drawing.Size(90, 22);
            lblremark.Text = "说明";
            lblremark.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            meremark.Location = new System.Drawing.Point(100, 207);
            meremark.Name = "meremark";
            meremark.Size = new System.Drawing.Size(180, 120);
            meremark.UseOptimizedRendering = true;

            groupControl1.Controls.Add(lblobjectId);
            groupControl1.Controls.Add(txtobjectId);
            groupControl1.Controls.Add(lblenglishName);
            groupControl1.Controls.Add(txtenglishName);
            groupControl1.Controls.Add(lblchineseName);
            groupControl1.Controls.Add(txtchineseName);
            groupControl1.Controls.Add(lblexistHisTable);
            groupControl1.Controls.Add(ckexistHisTable);
            groupControl1.Controls.Add(lblDB);
            groupControl1.Controls.Add(cbbDB);
            groupControl1.Controls.Add(lblversion);
            groupControl1.Controls.Add(txtversion);
            groupControl1.Controls.Add(lbllastupdate);
            groupControl1.Controls.Add(txtlastupdate);
            groupControl1.Controls.Add(lblremark);
            groupControl1.Controls.Add(meremark);

            groupControl1.Location = new System.Drawing.Point(3, 3);
            groupControl1.Name = "groupControl1";
            groupControl1.Size = new System.Drawing.Size(xtraTabPageBasic.ClientSize.Width - 30, xtraTabPageBasic.ClientSize.Height - 30);
            groupControl1.TabIndex = 5;
            groupControl1.Text = "基本信息";

            xtraTabPageBasic.Controls.Add(groupControl1);
            #endregion

            #region 基本信息初始化

            #region 绑定数据库类型
            List<CListItem> dbtypeList = new List<CListItem>();
            dbtypeList.Add(new CListItem("Oracle", "Oracle数据库"));
            dbtypeList.Add(new CListItem("Mysql", "Mysql数据库"));
            dbtypeList.Add(new CListItem("DB2", "DB2数据库"));
            dbtypeList.Add(new CListItem("SqlServer", "SqlServer数据库"));
            dbtypeList.Add(new CListItem("Sqlite", "Sqlite数据库"));
            dbtypeList.Add(new CListItem("Access", "Access数据库"));
            cbbDB.BindDictItems(dbtypeList);
            #endregion

            xmltablesinfohelper = new XmlHelper(string.Format(@"XML\{0}.table", item.Name));
            XmlNodeList xmlNodeLst = xmltablesinfohelper.Read(string.Format("datatype/basicinfo"));
            foreach (XmlNode xn1 in xmlNodeLst)
            {
                
                // 将节点转换为元素，便于得到节点的属性值
                XmlElement xe = (XmlElement)xn1;
                strBasicInfoGuid = xe.GetAttribute("guid").ToString();

                // 得到DataTypeInfo节点的所有子节点
                XmlNodeList xnl0 = xe.ChildNodes;
                txtobjectId.Text = xnl0.Item(0).InnerText;
                txtenglishName.Text = xnl0.Item(1).InnerText;
                txtchineseName.Text = xnl0.Item(2).InnerText;
                ckexistHisTable.Checked = xnl0.Item(3).InnerText == "1" ? true : false;
                cbbDB.SetComboBoxItem(xnl0.Item(4).InnerText);
                txtversion.Text = xnl0.Item(5).InnerText;
                txtlastupdate.Text = xnl0.Item(6).InnerText;
                meremark.Text = xnl0.Item(7).InnerText;
            }
            #endregion

            #region 基本信息修改
            _txtlastupdate = txtlastupdate;
            txtobjectId.Validated += new System.EventHandler(txtValue_Validated);
            txtenglishName.Validated += new System.EventHandler(txtValue_Validated);
            txtchineseName.Validated += new System.EventHandler(txtValue_Validated);
            ckexistHisTable.Validated += new System.EventHandler(txtValue_Validated);
            cbbDB.Validated += new System.EventHandler(txtValue_Validated);
            txtversion.Validated += new System.EventHandler(txtValue_Validated);
            txtlastupdate.Validated += new System.EventHandler(txtValue_Validated);
            meremark.Validated += new System.EventHandler(txtValue_Validated);

            #endregion

            #region 字段及索引
            xtraTabPageFields.Name = "xtraTabPageFields";
            xtraTabPageFields.Text = "字段及索引";

            SplitContainer splitContainer1 = new SplitContainer();
            GroupControl groupControlFields = new GroupControl();
            GroupControl groupControlIndexs = new GroupControl();

            ((System.ComponentModel.ISupportInitialize)(splitContainer1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(groupControlFields)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(groupControlIndexs)).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();

            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            splitContainer1.SplitterDistance = 413;

            #region 字段表格
            groupControlFields.SuspendLayout();
            groupControlFields.Dock = DockStyle.Fill;
            groupControlFields.Name = "groupControlFields";
            groupControlFields.TabIndex = 5;
            groupControlFields.Text = "字段";

            DevExpress.XtraGrid.GridControl gridControlFields = new DevExpress.XtraGrid.GridControl();
            gridViewFields = new DevExpress.XtraGrid.Views.Grid.GridView();
            DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit repositoryItemChkIsNull = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit repositoryItemLookUpEditFields = new DevExpress.XtraEditors.Repository.RepositoryItemLookUpEdit();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnGuid = new DevExpress.XtraGrid.Columns.GridColumn();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnFieldName = new DevExpress.XtraGrid.Columns.GridColumn();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnChineseName = new DevExpress.XtraGrid.Columns.GridColumn();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnFieldType = new DevExpress.XtraGrid.Columns.GridColumn();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnFieldInfo = new DevExpress.XtraGrid.Columns.GridColumn();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnIsNull = new DevExpress.XtraGrid.Columns.GridColumn();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnRemark = new DevExpress.XtraGrid.Columns.GridColumn();


            ((System.ComponentModel.ISupportInitialize)(gridControlFields)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(gridViewFields)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemChkIsNull)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemLookUpEditFields)).BeginInit();

            gridControlFields.Dock = DockStyle.Fill;
            gridControlFields.Cursor = System.Windows.Forms.Cursors.Default;
            gridControlFields.MainView = gridViewFields;
            gridControlFields.Name = "gridControlFields";
            gridControlFields.Size = new System.Drawing.Size(981, 573);
            gridControlFields.TabIndex = 13;
            gridControlFields.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            gridViewFields});
            gridControlFields.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            repositoryItemChkIsNull, repositoryItemLookUpEditFields});
            gridControlFields.ContextMenuStrip = contextMenuStripFields;

            gridViewFields.Appearance.FocusedRow.BackColor = System.Drawing.Color.LightCyan;
            gridViewFields.Appearance.FocusedRow.BackColor2 = System.Drawing.Color.LightCyan;
            gridViewFields.Appearance.FocusedRow.Options.UseBackColor = true;
            gridViewFields.Appearance.FocusedRow.Options.UseForeColor = true;
            gridViewFields.GridControl = gridControlFields;
            gridViewFields.Name = "gridViewFields";
            gridViewFields.OptionsBehavior.AllowAddRows = DevExpress.Utils.DefaultBoolean.False;
            gridViewFields.OptionsBehavior.AllowDeleteRows = DevExpress.Utils.DefaultBoolean.False;
            gridViewFields.OptionsCustomization.AllowFilter = false;
            gridViewFields.OptionsCustomization.AllowGroup = false;
            gridViewFields.OptionsMenu.EnableColumnMenu = false;
            gridViewFields.OptionsMenu.EnableFooterMenu = false;
            gridViewFields.OptionsMenu.EnableGroupPanelMenu = false;
            gridViewFields.OptionsView.EnableAppearanceEvenRow = true;
            gridViewFields.OptionsView.EnableAppearanceOddRow = true;
            gridViewFields.OptionsView.ShowGroupPanel = false;
            gridViewFields.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { gridColumnGuid, gridColumnFieldName, gridColumnChineseName, gridColumnFieldType, gridColumnFieldInfo, gridColumnIsNull, gridColumnRemark });
            gridViewFields.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(gridViewFields_CellValueChanged);
            gridViewFields.CellValueChanging += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(gridViewFields_CellValueChanging);
            gridViewFields.BeforeLeaveRow += new DevExpress.XtraGrid.Views.Base.RowAllowEventHandler(gridViewFields_BeforeLeaveRow);
            gridViewFields.ValidateRow += new DevExpress.XtraGrid.Views.Base.ValidateRowEventHandler(gridViewFields_ValidateRow);
            gridViewFields.DoubleClick += new System.EventHandler(gridViewFields_DoubleClick);

            gridColumnGuid.Caption = "GUID";
            gridColumnGuid.Name = "gridColumnGUID";
            gridColumnGuid.Visible = true;
            gridColumnGuid.VisibleIndex = 0;
            gridColumnGuid.FieldName = "GUID";

            gridColumnFieldName.Caption = "字段名";
            gridColumnFieldName.Name = "gridColumnFieldName";
            gridColumnFieldName.Visible = true;
            gridColumnFieldName.VisibleIndex = 0;
            gridColumnFieldName.FieldName = "FieldName";

            gridColumnChineseName.Caption = "中文名";
            gridColumnChineseName.Name = "gridColumnChineseName";
            gridColumnChineseName.Visible = true;
            gridColumnChineseName.VisibleIndex = 1;
            gridColumnChineseName.FieldName = "ChineseName";
            gridColumnChineseName.OptionsColumn.ReadOnly = true;

            gridColumnFieldType.Caption = "字段类型";
            gridColumnFieldType.Name = "gridColumnFieldType";
            gridColumnFieldType.Visible = true;
            gridColumnFieldType.VisibleIndex = 2;
            gridColumnFieldType.FieldName = "FieldType";
            gridColumnFieldType.OptionsColumn.ReadOnly = true;

            gridColumnFieldInfo.Caption = "字段说明";
            gridColumnFieldInfo.Name = "gridColumnFieldInfo";
            gridColumnFieldInfo.Visible = true;
            gridColumnFieldInfo.VisibleIndex = 3;
            gridColumnFieldInfo.FieldName = "FieldInfo";
            gridColumnFieldInfo.OptionsColumn.ReadOnly = true;

            gridColumnIsNull.Caption = "允许空";
            gridColumnIsNull.Name = "gridColumnIsNull";
            gridColumnIsNull.Visible = true;
            gridColumnIsNull.VisibleIndex = 4;
            gridColumnIsNull.FieldName = "IsNull";

            gridColumnRemark.Caption = "修改内容";
            gridColumnRemark.Name = "gridColumnRemark";
            gridColumnRemark.Visible = true;
            gridColumnRemark.VisibleIndex = 5;
            gridColumnRemark.FieldName = "Remark";

            repositoryItemChkIsNull.AutoHeight = false;
            repositoryItemChkIsNull.Caption = "Check";
            repositoryItemChkIsNull.Name = "repositoryItemChkIsNull";

            repositoryItemLookUpEditFields.PopupWidth = 400; //下拉框宽度  
            repositoryItemLookUpEditFields.NullText = "";//空时的值  
            repositoryItemLookUpEditFields.DropDownRows = 10;//下拉框行数  
            repositoryItemLookUpEditFields.ImmediatePopup = true;//输入值是否马上弹出窗体  
            repositoryItemLookUpEditFields.ValidateOnEnterKey = true;//回车确认  
            repositoryItemLookUpEditFields.SearchMode = SearchMode.AutoFilter;//自动过滤掉不需要显示的数据，可以根据需要变化  
            repositoryItemLookUpEditFields.TextEditStyle = TextEditStyles.Standard;//要使用户可以输入，这里须设为Standard  
            repositoryItemLookUpEditFields.AllowNullInput = DevExpress.Utils.DefaultBoolean.True; //可用Ctrl + Delete清空选择內容 
            repositoryItemLookUpEditFields.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            //添加显示列  
            repositoryItemLookUpEditFields.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] {  
                 new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Name", "字段名"),  
                 new DevExpress.XtraEditors.Controls.LookUpColumnInfo("ChineseName", "字段名称"),
                 new DevExpress.XtraEditors.Controls.LookUpColumnInfo("DataType", "字段类型"),
                 new DevExpress.XtraEditors.Controls.LookUpColumnInfo("DictNo", "字典条目"),
                 new DevExpress.XtraEditors.Controls.LookUpColumnInfo("DictNameLst", "字典条目说明"),
            });
            repositoryItemLookUpEditFields.ValueMember = "Name";
            repositoryItemLookUpEditFields.DisplayMember = "Name";
            repositoryItemLookUpEditFields.EditValueChanged += repositoryItemLookUpEditFields_EditValueChanged;

            ((System.ComponentModel.ISupportInitialize)(repositoryItemLookUpEditFields)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemChkIsNull)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(gridControlFields)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(gridViewFields)).EndInit();
            groupControlFields.Controls.Add(gridControlFields);
            gridControlFields.DataSource = new List<TableFieldsInfo>();
            #endregion

            #region 字段初始化

            XmlHelper stdfieldxmlHelper = new XmlHelper(@"XML\stdfield.xml");
            XmlNodeList stdfieldxmlNodeLst = stdfieldxmlHelper.Read("datatype/dataitem");
            
            List<StdFieldComboBox> stdFieldInfoList = new List<StdFieldComboBox>();
            foreach (XmlNode xn1 in stdfieldxmlNodeLst)
            {
                // 将节点转换为元素，便于得到节点的属性值
                XmlElement xe = (XmlElement)xn1;
                // 得到DataTypeInfo节点的所有子节点
                XmlNodeList xnl0 = xe.ChildNodes;
                StdFieldComboBox listItem = new StdFieldComboBox();
                listItem.Name = xnl0.Item(0).InnerText;
                listItem.ChineseName = xnl0.Item(1).InnerText;
                listItem.DataType = xnl0.Item(2).InnerText;
                listItem.DictNo = xnl0.Item(3).InnerText;
                if (dictTypeInfoList != null)
                {
                    var dictType = dictTypeInfoList.Find(new Predicate<DictInfo>(dictinfo => dictinfo.ID.ToString() == xnl0.Item(3).InnerText));
                    if (dictType != null) listItem.DictNameLst = dictType.Remark;
                }

                stdFieldInfoList.Add(listItem);
            }

            repositoryItemLookUpEditFields.DataSource = stdFieldInfoList;

            gridViewFields.Columns["IsNull"].ColumnEdit = repositoryItemChkIsNull;
            gridViewFields.Columns["FieldName"].ColumnEdit = repositoryItemLookUpEditFields;
            gridViewFields.Columns["GUID"].Visible = false;

            XmlNodeList xmlfieldsLst = xmltablesinfohelper.Read(string.Format("datatype/fieldsinfo"));
            List<TableFieldsInfo> FieldsInfoLst = new List<TableFieldsInfo>();

            foreach (XmlNode xn1 in xmlfieldsLst)
            {
                TableFieldsInfo tablefieldInfo = new TableFieldsInfo();

                // 将节点转换为元素，便于得到节点的属性值
                XmlElement xe = (XmlElement)xn1;

                tablefieldInfo.GUID = xe.GetAttribute("guid").ToString();

                // 得到DataTypeInfo节点的所有子节点
                XmlNodeList xnl0 = xe.ChildNodes;
                
                for(Int32 i = 0; i < stdFieldInfoList.Count; i ++)
                {
                    if (string.Equals(stdFieldInfoList[i].Name, xnl0.Item(0).InnerText))
                    {
                        tablefieldInfo.FieldName = stdFieldInfoList[i].Name;
                        tablefieldInfo.ChineseName = stdFieldInfoList[i].ChineseName;
                        tablefieldInfo.FieldType = stdFieldInfoList[i].DataType;
                        tablefieldInfo.FieldInfo = stdFieldInfoList[i].DictNameLst;
                        break;
                    }
                }

                tablefieldInfo.IsNull = xnl0.Item(1).InnerText == "0" ? false : true;
                tablefieldInfo.Remark = xnl0.Item(2).InnerText;
                tablefieldInfo.lstInfo = new Dictionary<string, DevExpress.XtraEditors.DXErrorProvider.ErrorInfo>();
                FieldsInfoLst.Add(tablefieldInfo);
            }

            gridControlFields.DataSource = FieldsInfoLst;
            #endregion

            #region 索引表格
            groupControlIndexs.SuspendLayout();
            groupControlIndexs.Dock = DockStyle.Fill;
            groupControlIndexs.Name = "groupControlIndexs";
            groupControlIndexs.TabIndex = 5;
            groupControlIndexs.Text = "索引";

            DevExpress.XtraGrid.GridControl gridControlIndexs = new DevExpress.XtraGrid.GridControl();
            gridViewIndexs = new DevExpress.XtraGrid.Views.Grid.GridView();
            DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit repositoryItemChkIsUnique = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit repositoryItemChkIsIndex = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit repositoryItemChkIsPrimary = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            DevExpress.XtraEditors.Repository.RepositoryItemCheckedComboBoxEdit repositoryItemCheckedComboBoxIndexFields = new DevExpress.XtraEditors.Repository.RepositoryItemCheckedComboBoxEdit();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnIndexGuid = new DevExpress.XtraGrid.Columns.GridColumn();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnIndexName = new DevExpress.XtraGrid.Columns.GridColumn();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnIndexFieldLst = new DevExpress.XtraGrid.Columns.GridColumn();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnIndexUnique = new DevExpress.XtraGrid.Columns.GridColumn();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnIndex = new DevExpress.XtraGrid.Columns.GridColumn();
            DevExpress.XtraGrid.Columns.GridColumn gridColumnIndexPrimary = new DevExpress.XtraGrid.Columns.GridColumn();

            ((System.ComponentModel.ISupportInitialize)(gridControlIndexs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(gridViewIndexs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemChkIsUnique)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemChkIsIndex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemChkIsPrimary)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemCheckedComboBoxIndexFields)).BeginInit();

            gridControlIndexs.Dock = DockStyle.Fill;
            gridControlIndexs.Cursor = System.Windows.Forms.Cursors.Default;
            gridControlIndexs.MainView = gridViewIndexs;
            gridControlIndexs.Name = "gridControlIndexs";
            gridControlIndexs.Size = new System.Drawing.Size(981, 573);
            gridControlIndexs.TabIndex = 13;
            gridControlIndexs.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            gridViewIndexs});
            gridControlIndexs.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            repositoryItemChkIsUnique, repositoryItemChkIsIndex, repositoryItemChkIsPrimary, repositoryItemCheckedComboBoxIndexFields});
            gridControlIndexs.ContextMenuStrip = contextMenuStripIndex;

            gridViewIndexs.Appearance.FocusedRow.BackColor = System.Drawing.Color.LightCyan;
            gridViewIndexs.Appearance.FocusedRow.BackColor2 = System.Drawing.Color.LightCyan;
            gridViewIndexs.Appearance.FocusedRow.Options.UseBackColor = true;
            gridViewIndexs.Appearance.FocusedRow.Options.UseForeColor = true;
            gridViewIndexs.GridControl = gridControlIndexs;
            gridViewIndexs.Name = "gridViewIndexs";
            gridViewIndexs.OptionsBehavior.AllowAddRows = DevExpress.Utils.DefaultBoolean.False;
            gridViewIndexs.OptionsBehavior.AllowDeleteRows = DevExpress.Utils.DefaultBoolean.False;
            gridViewIndexs.OptionsCustomization.AllowFilter = false;
            gridViewIndexs.OptionsCustomization.AllowGroup = false;
            gridViewIndexs.OptionsMenu.EnableColumnMenu = false;
            gridViewIndexs.OptionsMenu.EnableFooterMenu = false;
            gridViewIndexs.OptionsMenu.EnableGroupPanelMenu = false;
            gridViewIndexs.OptionsView.EnableAppearanceEvenRow = true;
            gridViewIndexs.OptionsView.EnableAppearanceOddRow = true;
            gridViewIndexs.OptionsView.ShowGroupPanel = false;
            gridViewIndexs.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { gridColumnIndexGuid, gridColumnIndexName, gridColumnIndexFieldLst, gridColumnIndexUnique, gridColumnIndex, gridColumnIndexPrimary });
            gridViewIndexs.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(gridViewIndexs_CellValueChanged);
            gridViewIndexs.CellValueChanging += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(gridViewIndexs_CellValueChanging);
            gridViewIndexs.BeforeLeaveRow += new DevExpress.XtraGrid.Views.Base.RowAllowEventHandler(gridViewIndexs_BeforeLeaveRow);
            gridViewIndexs.ValidateRow += new DevExpress.XtraGrid.Views.Base.ValidateRowEventHandler(gridViewIndexs_ValidateRow);
            gridViewIndexs.DoubleClick += new System.EventHandler(gridViewIndexs_DoubleClick);

            gridColumnIndexGuid.Caption = "GUID";
            gridColumnIndexGuid.Name = "gridColumnIndexGuid";
            gridColumnIndexGuid.Visible = true;
            gridColumnIndexGuid.VisibleIndex = 0;
            gridColumnIndexGuid.FieldName = "GUID";

            gridColumnIndexName.Caption = "索引名";
            gridColumnIndexName.Name = "gridColumnIndexName";
            gridColumnIndexName.Visible = true;
            gridColumnIndexName.VisibleIndex = 1;
            gridColumnIndexName.FieldName = "IndexName";

            gridColumnIndexFieldLst.Caption = "索引字段列表";
            gridColumnIndexFieldLst.Name = "gridColumnIndexFieldLst";
            gridColumnIndexFieldLst.Visible = true;
            gridColumnIndexFieldLst.VisibleIndex = 2;
            gridColumnIndexFieldLst.FieldName = "IndexFieldLst";

            gridColumnIndexUnique.Caption = "唯一";
            gridColumnIndexUnique.Name = "gridColumnIndexUnique";
            gridColumnIndexUnique.Visible = true;
            gridColumnIndexUnique.VisibleIndex = 3;
            gridColumnIndexUnique.FieldName = "Unique";

            gridColumnIndex.Caption = "索引";
            gridColumnIndex.Name = "gridColumnIndex";
            gridColumnIndex.Visible = true;
            gridColumnIndex.VisibleIndex = 3;
            gridColumnIndex.FieldName = "Index";

            gridColumnIndexPrimary.Caption = "主键";
            gridColumnIndexPrimary.Name = "gridColumnIndexPrimary";
            gridColumnIndexPrimary.Visible = true;
            gridColumnIndexPrimary.VisibleIndex = 4;
            gridColumnIndexPrimary.FieldName = "Primary";

            repositoryItemChkIsUnique.AutoHeight = false;
            repositoryItemChkIsUnique.Caption = "Check";
            repositoryItemChkIsUnique.Name = "repositoryItemChkIsUnique";

            repositoryItemChkIsIndex.AutoHeight = false;
            repositoryItemChkIsIndex.Caption = "Check";
            repositoryItemChkIsIndex.Name = "repositoryItemChkIsIndex";

            repositoryItemChkIsPrimary.AutoHeight = false;
            repositoryItemChkIsPrimary.Caption = "Check";
            repositoryItemChkIsPrimary.Name = "repositoryItemChkIsPrimary";

            repositoryItemCheckedComboBoxIndexFields.NullText = "";//空时的值  
            repositoryItemCheckedComboBoxIndexFields.ValidateOnEnterKey = true;//回车确认  
            repositoryItemCheckedComboBoxIndexFields.TextEditStyle = TextEditStyles.Standard;//要使用户可以输入，这里须设为Standard  
            repositoryItemCheckedComboBoxIndexFields.AllowNullInput = DevExpress.Utils.DefaultBoolean.True; //可用Ctrl + Delete清空选择內容 
            repositoryItemCheckedComboBoxIndexFields.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            //添加显示列  
            repositoryItemCheckedComboBoxIndexFields.Items.AddRange(new DevExpress.XtraEditors.Controls.CheckedListBoxItem[] {  
                 new DevExpress.XtraEditors.Controls.CheckedListBoxItem("FieldName", "字段名"),  
                 new DevExpress.XtraEditors.Controls.CheckedListBoxItem("ChineseName", "字段名称"),
            });
            repositoryItemCheckedComboBoxIndexFields.ValueMember = "FieldName";
            repositoryItemCheckedComboBoxIndexFields.DisplayMember = "FieldName";
            repositoryItemCheckedComboBoxIndexFields.EditValueChanged += repositoryItemLookUpEditIndexFields_EditValueChanged;
            ((System.ComponentModel.ISupportInitialize)(repositoryItemCheckedComboBoxIndexFields)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemChkIsUnique)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemChkIsIndex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemChkIsPrimary)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(gridControlIndexs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(gridViewIndexs)).EndInit();

            groupControlIndexs.Controls.Add(gridControlIndexs);
            gridControlIndexs.DataSource = new List<TableIndexsInfo>();


            #endregion

            splitContainer1.Panel1.Controls.Add(groupControlFields);
            splitContainer1.Panel2.Controls.Add(groupControlIndexs);

            xtraTabPageFields.Controls.Add(splitContainer1);

            #endregion

            #region 索引初始化
            repositoryItemCheckedComboBoxIndexFields.DataSource = FieldsInfoLst;

            gridViewIndexs.Columns["Unique"].ColumnEdit = repositoryItemChkIsUnique;
            gridViewIndexs.Columns["Index"].ColumnEdit = repositoryItemChkIsIndex;
            gridViewIndexs.Columns["Primary"].ColumnEdit = repositoryItemChkIsPrimary;
            gridViewIndexs.Columns["IndexFieldLst"].ColumnEdit = repositoryItemCheckedComboBoxIndexFields;
            gridViewIndexs.Columns["GUID"].Visible = false;

            XmlNodeList xmlindexLst = xmltablesinfohelper.Read(string.Format("datatype/indexsinfo"));

            List<TableIndexsInfo> IndexsInfoLst = new List<TableIndexsInfo>();

            foreach (XmlNode xn1 in xmlindexLst)
            {
                TableIndexsInfo tableindexsInfo = new TableIndexsInfo();

                // 将节点转换为元素，便于得到节点的属性值
                XmlElement xe = (XmlElement)xn1;
                tableindexsInfo.GUID = xe.GetAttribute("guid").ToString();

                // 得到DataTypeInfo节点的所有子节点
                XmlNodeList xnl0 = xe.ChildNodes;
                tableindexsInfo.IndexName = xnl0.Item(0).InnerText;
                tableindexsInfo.IndexFieldLst = xnl0.Item(1).InnerText;
                tableindexsInfo.Unique = xnl0.Item(2).InnerText == "0" ? false : true;
                tableindexsInfo.Index = xnl0.Item(3).InnerText == "0" ? false : true;
                tableindexsInfo.Primary = xnl0.Item(4).InnerText == "0" ? false : true;
                tableindexsInfo.lstInfo = new Dictionary<string, DevExpress.XtraEditors.DXErrorProvider.ErrorInfo>();
                IndexsInfoLst.Add(tableindexsInfo);
            }

            gridControlIndexs.DataSource = IndexsInfoLst;
            #endregion

            #region SQL预览
            xtraTabPageSQLLook.Name = "xtraTabPageSQLLook";
            xtraTabPageSQLLook.Text = "SQL预览";

            richEditControl = new DevExpress.XtraRichEdit.RichEditControl();
            richEditControl.ActiveViewType = DevExpress.XtraRichEdit.RichEditViewType.Simple;
            richEditControl.Dock = System.Windows.Forms.DockStyle.Fill;
            richEditControl.EnableToolTips = true;
            richEditControl.Location = new System.Drawing.Point(2, 2);
            richEditControl.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            richEditControl.Name = "richEditControl";
            richEditControl.Options.AutoCorrect.DetectUrls = false;
            richEditControl.Options.AutoCorrect.ReplaceTextAsYouType = false;
            richEditControl.Options.Behavior.PasteLineBreakSubstitution = DevExpress.XtraRichEdit.LineBreakSubstitute.Paragraph;
            richEditControl.Options.CopyPaste.MaintainDocumentSectionSettings = false;
            richEditControl.Options.DocumentCapabilities.Bookmarks = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.CharacterStyle = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.HeadersFooters = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.Hyperlinks = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.InlinePictures = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.Numbering.Bulleted = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.Numbering.MultiLevel = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.Numbering.Simple = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.ParagraphFormatting = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.Paragraphs = DevExpress.XtraRichEdit.DocumentCapability.Enabled;
            richEditControl.Options.DocumentCapabilities.ParagraphStyle = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.Sections = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.Tables = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.DocumentCapabilities.TableStyle = DevExpress.XtraRichEdit.DocumentCapability.Disabled;
            richEditControl.Options.Fields.UseCurrentCultureDateTimeFormat = false;
            richEditControl.Options.HorizontalRuler.Visibility = DevExpress.XtraRichEdit.RichEditRulerVisibility.Hidden;
            richEditControl.Options.MailMerge.KeepLastParagraph = false;
            richEditControl.Size = new System.Drawing.Size(1111, 627);
            richEditControl.TabIndex = 1;
            richEditControl.Text = "richEditControl1";
            richEditControl.Views.SimpleView.Padding = new System.Windows.Forms.Padding(55, 4, 0, 0);
            richEditControl.Views.DraftView.Padding = new System.Windows.Forms.Padding(55, 4, 0, 0);
            richEditControl.Views.SimpleView.AllowDisplayLineNumbers = true;
            richEditControl.Views.DraftView.AllowDisplayLineNumbers = true;
            richEditControl.Document.Sections[0].LineNumbering.Start = 1;
            richEditControl.Document.Sections[0].LineNumbering.CountBy = 2;
            richEditControl.Document.Sections[0].LineNumbering.RestartType = DevExpress.XtraRichEdit.API.Native.LineNumberingRestart.Continuous;
            richEditControl.Document.CharacterStyles["Line Number"].FontName = "Courier";
            richEditControl.Document.CharacterStyles["Line Number"].FontSize = 10;
            richEditControl.Document.CharacterStyles["Line Number"].ForeColor = Color.DarkGray;
            richEditControl.Document.CharacterStyles["Line Number"].Bold = true;

            richEditControl.InitializeDocument += new System.EventHandler(richEditControl_InitializeDocument);
            richEditControl.AddService(typeof(ISyntaxHighlightService), new SyntaxHighlightService(richEditControl));
            IRichEditCommandFactoryService commandFactory = richEditControl.GetService<IRichEditCommandFactoryService>();
            CustomRichEditCommandFactoryService newCommandFactory = new CustomRichEditCommandFactoryService(commandFactory);
            richEditControl.RemoveService(typeof(IRichEditCommandFactoryService));
            richEditControl.AddService(typeof(IRichEditCommandFactoryService), newCommandFactory);

            IDocumentImportManagerService importManager = richEditControl.GetService<IDocumentImportManagerService>();
            importManager.UnregisterAllImporters();
            importManager.RegisterImporter(new PlainTextDocumentImporter());
            importManager.RegisterImporter(new SourcesCodeDocumentImporter());

            IDocumentExportManagerService exportManager = richEditControl.GetService<IDocumentExportManagerService>();
            exportManager.UnregisterAllExporters();
            exportManager.RegisterExporter(new PlainTextDocumentExporter());
            exportManager.RegisterExporter(new SourcesCodeDocumentExporter());

            if (FileUtil.IsExistFile("SQL.tmp"))
            {
                FileUtil.DeleteFile("SQL.tmp");
            }
            FileUtil.CreateFile("SQL.tmp");

            cbbDB_SelectedValueChanged(cbbDB, null);

            string dbType = cbbDB.GetComboBoxStrValue();
            #region 先读取datatype.xml 在读取defaulttype.xml 然后Linq 查询保存到数据字典dic中
            XmlHelper xmldatatypehelper = new XmlHelper(@"XML\datatype.xml");
            XmlNodeList xmldatatypeNodeLst = xmldatatypehelper.Read("datatype");
            List<DataTypeInfo> dataTypeInfoList = new List<DataTypeInfo>();
            foreach (XmlNode xn1 in xmldatatypeNodeLst)
            {
                DataTypeInfo dataTypeInfo = new DataTypeInfo();
                // 将节点转换为元素，便于得到节点的属性值
                XmlElement xe = (XmlElement)xn1;
                // 得到Type和ISBN两个属性的属性值
                dataTypeInfo.GUID = xe.GetAttribute("guid").ToString();

                // 得到DataTypeInfo节点的所有子节点
                XmlNodeList xnl0 = xe.ChildNodes;
                dataTypeInfo.Name = xnl0.Item(0).InnerText;
                dataTypeInfo.StdType = xnl0.Item(2).InnerText;
                dataTypeInfo.Length = xnl0.Item(3).InnerText;
                dataTypeInfo.Precision = xnl0.Item(4).InnerText;

                dataTypeInfoList.Add(dataTypeInfo);
            }

            XmlHelper defaulttypexmlHelper = new XmlHelper(@"XML\defaulttype.xml");
            XmlNodeList defaulttypexmlNodeLst = defaulttypexmlHelper.Read("datatype");
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var dataTypeInfo in dataTypeInfoList)
            {
                foreach (XmlNode xn1 in defaulttypexmlNodeLst)
                {
                    // 将节点转换为元素，便于得到节点的属性值
                    XmlElement xe = (XmlElement)xn1;
                    // 得到DataTypeInfo节点的所有子节点
                    XmlNodeList xnl0 = xe.ChildNodes;
                    string value = string.Empty;
                    if (dbType == "Oracle")
                        value = xnl0.Item(2).InnerText;
                    else if (dbType == "Mysql")
                        value = xnl0.Item(3).InnerText;
                    else if (dbType == "DB2")
                        value = xnl0.Item(4).InnerText;
                    else if (dbType == "SqlServer")
                        value = xnl0.Item(5).InnerText;
                    else if (dbType == "Sqlite")
                        value = xnl0.Item(6).InnerText;
                    else if (dbType == "Access")
                        value = xnl0.Item(7).InnerText;

                    // 找到匹配记录
                    if (dataTypeInfo.StdType == xnl0.Item(0).InnerText)
                    {
                        if (value.Contains("$L"))
                        {
                            if (String.Empty == dataTypeInfo.Length)
                                value = value.Replace("$L", "0");
                            else
                                value = value.Replace("$L", dataTypeInfo.Length);

                        }
                        if (value.Contains("$P"))
                        {
                            if (String.Empty == dataTypeInfo.Precision)
                                value = value.Replace("$P", "0");
                            else
                                value = value.Replace("$P", dataTypeInfo.Precision);
                        }
                        dict.Add(dataTypeInfo.Name, value);
                    }
                }
            }
            #endregion

            FileUtil.AppendText(@"SQL.tmp", JCodes.Framework.Common.Proj.SqlOperate.initTableInfo(cbbDB.GetComboBoxStrValue(), txtenglishName.Text, txtchineseName.Text, ckexistHisTable.Checked, FieldsInfoLst, IndexsInfoLst, dict, true), Encoding.Default);

            richEditControl.LoadDocument(@"SQL.tmp", DocumentFormat.PlainText);
            xtraTabPageSQLLook.Controls.Add(richEditControl);
            #endregion

            #region 修订记录
            xtraTabPageHistoryRecord.Name = "xtraTabPageHistoryRecord";
            xtraTabPageHistoryRecord.Text = "修订记录";

            DevExpress.XtraEditors.Repository.RepositoryItemDateEdit repositoryItemDateEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemDateEdit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemDateEdit1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemDateEdit1.CalendarTimeProperties)).BeginInit();
            repositoryItemDateEdit1.AutoHeight = false;
            repositoryItemDateEdit1.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            repositoryItemDateEdit1.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            repositoryItemDateEdit1.Name = "repositoryItemDateEdit1";
            ((System.ComponentModel.ISupportInitialize)(repositoryItemDateEdit1.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(repositoryItemDateEdit1)).EndInit();

            CommonControl.Pager.WinGridViewPager gridViewModrecord = new CommonControl.Pager.WinGridViewPager();
            gridViewModrecord.DisplayColumns = "";
            gridViewModrecord.Dock = DockStyle.Fill;
            gridViewModrecord.FixedColumns = null;
            gridViewModrecord.Location = new System.Drawing.Point(0, 0);
            gridViewModrecord.MinimumSize = new System.Drawing.Size(540, 0);
            gridViewModrecord.Name = "gridViewModrecord";
            gridViewModrecord.PrintTitle = "";
            gridViewModrecord.ShowAddMenu = true;
            gridViewModrecord.ShowCheckBox = false;
            gridViewModrecord.ShowDeleteMenu = true;
            gridViewModrecord.ShowEditMenu = true;
            gridViewModrecord.ShowExportButton = true;
            gridViewModrecord.Size = new System.Drawing.Size(941, 549);
            gridViewModrecord.TabIndex = 0;
            gridViewModrecord.gridControl1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { repositoryItemDateEdit1 });
            gridViewModrecord.DisplayColumns = "ModDate,ModVersion,ModOrderId,Proposer,Programmer,ModContent,ModReason,Remark";
            gridViewModrecord.AddColumnAlias("GUID", "GUID");
            gridViewModrecord.AddColumnAlias("ModDate", "修改日期");
            gridViewModrecord.AddColumnAlias("ModVersion", "修改版本");
            gridViewModrecord.AddColumnAlias("ModOrderId", "修改单号");
            gridViewModrecord.AddColumnAlias("Proposer", "申请人");
            gridViewModrecord.AddColumnAlias("Programmer", "修改人");
            gridViewModrecord.AddColumnAlias("ModContent", "修改内容");
            gridViewModrecord.AddColumnAlias("ModReason", "修改原因");
            gridViewModrecord.AddColumnAlias("Remark", "备注");
            List<ModRecordInfo> modRecordInfoLst = new List<ModRecordInfo>();
            gridViewModrecord.DataSource = modRecordInfoLst;
            xtraTabPageHistoryRecord.Controls.Add(gridViewModrecord);

            #endregion

            control.Controls.Add(xtraTabControl1);

            ((System.ComponentModel.ISupportInitialize)(groupControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(groupControlFields)).EndInit();
            groupControlFields.ResumeLayout(false);
            groupControlFields.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(groupControlIndexs)).EndInit();
            groupControlIndexs.ResumeLayout(false);
            groupControlIndexs.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(splitContainer1)).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(txtobjectId.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(txtenglishName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(txtchineseName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(txtversion.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(txtlastupdate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(ckexistHisTable.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(cbbDB.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(meremark.Properties)).EndInit();

            groupControl1.ResumeLayout(false);
            groupControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(xtraTabControl1)).EndInit();
            xtraTabControl1.ResumeLayout(false);

            control.Name = item.Hint;
            control.Text = item.Hint;

            BaseDocument document = tabbedView.AddDocument(control);
            document.Footer = Directory.GetCurrentDirectory();
            document.Tag = item.Hint;

            tabbedView.EndUpdate();
            tabbedView.Controller.Activate(document);

            dockPanel6.HideSliding();
        }

        // 数据库类型变更
        void cbbDB_SelectedValueChanged(object sender, EventArgs e)
        {
            // TODO
        }

        /// <summary>  
        /// 实现用户自由输入  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        private void repositoryItemLookUpEditFields_EditValueChanged(object sender, EventArgs e)
        {
            LookUpEdit edit = sender as LookUpEdit;
            if (edit.EditValue != null)
            {
                //取资料行，数据源为DataTable, 资料行是DataRowView对象。   
                StdFieldComboBox o = edit.Properties.GetDataSourceRowByKeyValue(edit.EditValue) as StdFieldComboBox;
                if (o != null)
                {
                    var tablefieldsInfo = gridViewFields.GetFocusedRow() as TableFieldsInfo;
                    tablefieldsInfo.FieldName = o.Name;
                    tablefieldsInfo.ChineseName = o.ChineseName;
                    tablefieldsInfo.FieldType = o.DataType;
                    tablefieldsInfo.FieldInfo = o.DictNameLst;

                    XmlNodeList xmlNodeLst = xmltablesinfohelper.Read("datatype/fieldsinfo/item[@guid=\"" + tablefieldsInfo.GUID + "\"]");
                    xmlNodeLst.Item(0).InnerText = o.Name;
                    xmltablesinfohelper.Save(false);

                    gridViewFields.RefreshRow(gridViewFields.FocusedRowHandle);
                }
            }
        }

        /// <summary>
        /// 验证正确修改其对应的xml值 并更新其更新时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtValue_Validated(object sender, EventArgs e)
        {
            Control c = sender as Control;
            string result = string.Empty;
            string value = string.Empty;

            // 判断版本手工修改是否符合格式
            if (string.Equals(c.Name, "txtversion"))
            {
                try
                {
                    new Version(c.Text);
                }
                catch (Exception ex)
                {
                    MessageDxUtil.ShowError(ex.Message);
                    return;
                }
            }

            if (string.IsNullOrEmpty(strBasicInfoGuid))
                return;

            switch (c.Name)
            {
                case "txtobjectId":
                    result = "datatype/basicinfo/item[@guid=\"" + strBasicInfoGuid + "\"]/functionId";
                    break;
                case "txtenglishName":
                    result = "datatype/basicinfo/item[@guid=\"" + strBasicInfoGuid + "\"]/name";
                    break;
                case "txtchineseName":
                    result = "datatype/basicinfo/item[@guid=\"" + strBasicInfoGuid + "\"]/chineseName";
                    break;
                case "ckexistHisTable":
                    result = "datatype/basicinfo/item[@guid=\"" + strBasicInfoGuid + "\"]/existhistable";
                    break;
                case "cbbDB":
                    result = "datatype/basicinfo/item[@guid=\"" + strBasicInfoGuid + "\"]/dbtype";
                    break;
                case "txtversion":
                    result = "datatype/basicinfo/item[@guid=\"" + strBasicInfoGuid + "\"]/version";
                    break;
                case "txtlastupdate":
                    result = "datatype/basicinfo/item[@guid=\"" + strBasicInfoGuid + "\"]/lasttime";
                    break;
                case "meremark":
                    result = "datatype/basicinfo/item[@guid=\"" + strBasicInfoGuid + "\"]/remark";
                    break;
            }

            // 如果不在这些已知的控件中则停止
            if (string.IsNullOrEmpty(result))
            {
                MessageDxUtil.ShowError("修改失败，其修改的控件不在已知范围内");
                return;
            }

            string curdatetime = DateTimeHelper.GetServerDateTime();
            xmltablesinfohelper.Replace(result, c.Name == "cbbDB" ? c.Text.Split('-')[0] : c.Text);
            xmltablesinfohelper.Replace("datatype/basicinfo/item[@guid=\"" + strBasicInfoGuid + "\"]/lasttime", curdatetime);
            xmltablesinfohelper.Save(false);

            _txtlastupdate.Text = curdatetime;
        }

        /// <summary>
        /// 加载数据字典
        /// </summary>
        private void LoadDicData()
        {
            #region 加载数据字典大项
            XmlHelper xmldicthelper = new XmlHelper(@"XML\dict.xml");
            XmlNodeList xmlNodeLst = xmldicthelper.Read("datatype/dataitem");
            dictTypeInfoList = new List<DictInfo>();
            foreach (XmlNode xn1 in xmlNodeLst)
            {
                DictInfo dictInfo = new DictInfo();
                // 将节点转换为元素，便于得到节点的属性值
                XmlElement xe = (XmlElement)xn1;

                // 得到DataTypeInfo节点的所有子节点
                XmlNodeList xnl0 = xe.ChildNodes;
                dictInfo.ID = Convert.ToInt32(xnl0.Item(0).InnerText);
                dictInfo.PID = Convert.ToInt32(xnl0.Item(1).InnerText);
                dictInfo.Name = xnl0.Item(2).InnerText;

                StringBuilder sb = new StringBuilder();

                XmlNodeList xmlNodeLst2 = xmldicthelper.Read(string.Format("datatype/dataitem/item[id=\"{0}\"]/subdic", dictInfo.ID));

                List<DictDetailInfo> dictDetailInfoList = new List<DictDetailInfo>();
                foreach (XmlNode xn12 in xmlNodeLst2)
                {
                    // 将节点转换为元素，便于得到节点的属性值
                    XmlElement xe2 = (XmlElement)xn12;

                    // 得到DataTypeInfo节点的所有子节点
                    XmlNodeList xnl02 = xe2.ChildNodes;
                    sb.Append(string.Format("{0}-{1},\r\n", xnl02.Item(0).InnerText, xnl02.Item(1).InnerText));
                }

                dictInfo.Remark = sb.ToString();

                dictTypeInfoList.Add(dictInfo);
            }
            #endregion
        }

        /// <summary>
        /// 新增字段
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem_AddField_Click(object sender, EventArgs e)
        {
            var tableFieldsInfo = new TableFieldsInfo();
            tableFieldsInfo.GUID = System.Guid.NewGuid().ToString();
            tableFieldsInfo.lstInfo = new Dictionary<string, DevExpress.XtraEditors.DXErrorProvider.ErrorInfo>();

            xmltablesinfohelper.InsertElement("datatype/fieldsinfo", "item", "guid", tableFieldsInfo.GUID, string.Format(xmlfieldsinfomodel, string.Empty, string.Empty, string.Empty));
            xmltablesinfohelper.Save(false);

            (gridViewFields.DataSource as List<TableFieldsInfo>).Add(tableFieldsInfo);
            gridViewFields.RefreshData();
        }

        /// <summary>
        /// 删除字段
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem_DelField_Click(object sender, EventArgs e)
        {
            // 20170824 如果是最后一行空行则不再继续操作
            if (gridViewFields.GetFocusedRow() as TableFieldsInfo == null || string.IsNullOrEmpty((gridViewFields.GetFocusedRow() as TableFieldsInfo).GUID))
                return;

            xmltablesinfohelper.DeleteByPathNode("datatype/fieldsinfo/item[@guid=\"" + gridViewFields.GetRowCellDisplayText(gridViewFields.FocusedRowHandle, "GUID") + "\"]");
            xmltablesinfohelper.Save(false);

            (gridViewFields.DataSource as List<TableFieldsInfo>).RemoveAt(gridViewFields.FocusedRowHandle);
            gridViewFields.RefreshData();
        }

        private void gridViewFields_DoubleClick(object sender, EventArgs e)
        {
            gridViewFields.OptionsBehavior.Editable = true;
        }

        private void gridViewFields_BeforeLeaveRow(object sender, DevExpress.XtraGrid.Views.Base.RowAllowEventArgs e)
        {
            gridViewFields.OptionsBehavior.Editable = false;
        }

        private void gridViewFields_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            XmlNodeList xmlNodeLst = xmltablesinfohelper.Read("datatype/fieldsinfo/item[@guid=\"" + tmptableFieldsInfo.GUID + "\"]");
            Int32 idx = -1;

            switch (e.Column.ToString())
            {
                case "字段名":
                    idx = 0;
                    break;
                case "允许空":
                    idx = 1;
                    break;
                case "修改内容":
                    idx = 2;
                    break;
            }

            if (idx == -1)
                return;

            // 20171106 特殊处理 对于勾选框0表示不勾选false，1表示勾选true
            if (1 == idx)
                xmlNodeLst.Item(idx).InnerText = Convert.ToBoolean(e.Value) ? "1" : "0";
            else
                xmlNodeLst.Item(idx).InnerText = e.Value.ToString();

            xmltablesinfohelper.Save(false);

            tmptableFieldsInfo = null;
        }

        private void gridViewFields_CellValueChanging(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            // 20170824 如果是最后一行空行则不再继续操作
            if (string.IsNullOrEmpty((gridViewFields.GetFocusedRow() as TableFieldsInfo).GUID))
                return;

            tmptableFieldsInfo = gridViewFields.GetRow(e.RowHandle) as TableFieldsInfo;
        }

        private void gridViewFields_ValidateRow(object sender, DevExpress.XtraGrid.Views.Base.ValidateRowEventArgs e)
        {
            // 查询是否存在2个键值的数据
            List<TableFieldsInfo> lsttableFieldsInfo = gridViewFields.DataSource as List<TableFieldsInfo>;

            // 查找重复的Name的值 && 清楚原先的错误信息

            List<String> tmpName = new List<string>();
            List<String> lstName = new List<string>();
            foreach (TableFieldsInfo tableFieldsInfo in lsttableFieldsInfo)
            {
                if (string.IsNullOrEmpty(tableFieldsInfo.GUID))
                    continue;

                if (lstName.Contains(tableFieldsInfo.FieldName))
                {
                    tmpName.Add(tableFieldsInfo.FieldName);
                }

                lstName.Add(tableFieldsInfo.FieldName);

                tableFieldsInfo.lstInfo.Clear();
            }

            foreach (TableFieldsInfo tableFieldsInfo in lsttableFieldsInfo)
            {
                if (string.IsNullOrEmpty(tableFieldsInfo.GUID))
                    continue;

                // 判断重复的 类型名
                if (tmpName.Contains(tableFieldsInfo.FieldName))
                {
                    if (tableFieldsInfo.lstInfo.ContainsKey("FieldName"))
                    {
                        tableFieldsInfo.lstInfo["FieldName"].ErrorText = tableFieldsInfo.lstInfo["FieldName"].ErrorText + "\r\n一个表中不允许存在重复的字段名";
                        tableFieldsInfo.lstInfo["FieldName"].ErrorType = tableFieldsInfo.lstInfo["FieldName"].ErrorType >= DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical ? tableFieldsInfo.lstInfo["FieldName"].ErrorType : DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                    }
                    else
                    {
                        tableFieldsInfo.lstInfo.Add("FieldName", new DevExpress.XtraEditors.DXErrorProvider.ErrorInfo("一个表中不允许存在重复的字段名", DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical));
                    }
                }

                // 判断名称是否为空
                if (string.IsNullOrEmpty(tableFieldsInfo.FieldName))
                {
                    if (tableFieldsInfo.lstInfo.ContainsKey("FieldName"))
                    {
                        tableFieldsInfo.lstInfo["FieldName"].ErrorText = tableFieldsInfo.lstInfo["FieldName"].ErrorText + "\r\n字段名不能为空";
                        tableFieldsInfo.lstInfo["FieldName"].ErrorType = tableFieldsInfo.lstInfo["FieldName"].ErrorType >= DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical ? tableFieldsInfo.lstInfo["FieldName"].ErrorType : DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                    }
                    else
                    {
                        tableFieldsInfo.lstInfo.Add("FieldName", new DevExpress.XtraEditors.DXErrorProvider.ErrorInfo("字段名不能为空", DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical));
                    }
                }
            }
        }


        /// <summary>
        /// 新增索引
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem_AddIndex_Click(object sender, EventArgs e)
        {
            var tableIndexsInfo = new TableIndexsInfo();
            tableIndexsInfo.GUID = System.Guid.NewGuid().ToString();
            tableIndexsInfo.lstInfo = new Dictionary<string, DevExpress.XtraEditors.DXErrorProvider.ErrorInfo>();

            xmltablesinfohelper.InsertElement("datatype/indexsinfo", "item", "guid", tableIndexsInfo.GUID, string.Format(xmlindexsinfomodel, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));
            xmltablesinfohelper.Save(false);

            (gridViewIndexs.DataSource as List<TableIndexsInfo>).Add(tableIndexsInfo);
            gridViewIndexs.RefreshData();
        }

        /// <summary>
        /// 删除索引
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem_DelIndex_Click(object sender, EventArgs e)
        {
            // 20171106 wjm 修复删除没有数据报错问题
            // 20170824 如果是最后一行空行则不再继续操作
            if (gridViewIndexs.GetFocusedRow() as TableIndexsInfo == null || string.IsNullOrEmpty((gridViewIndexs.GetFocusedRow() as TableIndexsInfo).GUID))
                return;

            xmltablesinfohelper.DeleteByPathNode("datatype/indexsinfo/item[@guid=\"" + gridViewIndexs.GetRowCellDisplayText(gridViewIndexs.FocusedRowHandle, "GUID") + "\"]");
            xmltablesinfohelper.Save(false);

            (gridViewIndexs.DataSource as List<TableIndexsInfo>).RemoveAt(gridViewIndexs.FocusedRowHandle);
            gridViewIndexs.RefreshData();
        }

        /// <summary>  
        /// 实现用户自由输入  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        private void repositoryItemLookUpEditIndexFields_EditValueChanged(object sender, EventArgs e)
        {
            DevExpress.XtraEditors.CheckedComboBoxEdit edit = sender as DevExpress.XtraEditors.CheckedComboBoxEdit;
            if (edit.EditValue != null)
            {
                XmlNodeList xmlNodeLst = xmltablesinfohelper.Read("datatype/indexsinfo/item[@guid=\"" + (gridViewIndexs.GetFocusedRow() as TableIndexsInfo).GUID + "\"]");
                xmlNodeLst.Item(1).InnerText = edit.EditValue.ToString();
                xmltablesinfohelper.Save(false);
            }
        }

        private void gridViewIndexs_DoubleClick(object sender, EventArgs e)
        {
            gridViewIndexs.OptionsBehavior.Editable = true;
        }

        private void gridViewIndexs_BeforeLeaveRow(object sender, DevExpress.XtraGrid.Views.Base.RowAllowEventArgs e)
        {
            gridViewIndexs.OptionsBehavior.Editable = false;
        }

        private void gridViewIndexs_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            XmlNodeList xmlNodeLst = xmltablesinfohelper.Read("datatype/indexsinfo/item[@guid=\"" + tmptableIndexsInfo.GUID + "\"]");
            Int32 idx = -1;

            switch (e.Column.ToString())
            {
                case "索引名":
                    idx = 0;
                    break;
                case "索引字段列表":
                    idx = 1;
                    break;
                case "唯一":
                    idx = 2;
                    break;
                case "索引":
                    idx = 3;
                    break;
                case "主键":
                    idx = 4;
                    break;
            }

            if (idx == -1)
                return;

            // 20171106 特殊处理 对于勾选框0表示不勾选false，1表示勾选true
            if (2 == idx || 3 == idx || 4 == idx)
                xmlNodeLst.Item(idx).InnerText = Convert.ToBoolean(e.Value) ? "1" : "0";
            else
                xmlNodeLst.Item(idx).InnerText = e.Value.ToString();

            xmltablesinfohelper.Save(false);

            tmptableIndexsInfo = null;
        }

        private void gridViewIndexs_CellValueChanging(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            // 20170824 如果是最后一行空行则不再继续操作
            if (string.IsNullOrEmpty((gridViewIndexs.GetFocusedRow() as TableIndexsInfo).GUID))
                return;

            tmptableIndexsInfo = gridViewIndexs.GetRow(e.RowHandle) as TableIndexsInfo;
        }

        private void gridViewIndexs_ValidateRow(object sender, DevExpress.XtraGrid.Views.Base.ValidateRowEventArgs e)
        {
            // 查询是否存在2个键值的数据
            List<TableIndexsInfo> lsttableIndexsInfo = gridViewIndexs.DataSource as List<TableIndexsInfo>;

            // 查找重复的Name的值 && 清楚原先的错误信息

            List<String> tmpName = new List<string>();
            List<String> lstName = new List<string>();
            foreach (TableIndexsInfo tableIndexsInfo in lsttableIndexsInfo)
            {
                if (string.IsNullOrEmpty(tableIndexsInfo.GUID))
                    continue;

                if (lstName.Contains(tableIndexsInfo.IndexName))
                {
                    tmpName.Add(tableIndexsInfo.IndexName);
                }

                lstName.Add(tableIndexsInfo.IndexName);

                tableIndexsInfo.lstInfo.Clear();
            }

            foreach (TableIndexsInfo tableIndexsInfo in lsttableIndexsInfo)
            {
                if (string.IsNullOrEmpty(tableIndexsInfo.GUID))
                    continue;

                // 判断重复的 类型名
                if (tmpName.Contains(tableIndexsInfo.IndexName))
                {
                    if (tableIndexsInfo.lstInfo.ContainsKey("IndexName"))
                    {
                        tableIndexsInfo.lstInfo["IndexName"].ErrorText = tableIndexsInfo.lstInfo["IndexName"].ErrorText + "\r\n一个表中不允许存在重复的索引名";
                        tableIndexsInfo.lstInfo["IndexName"].ErrorType = tableIndexsInfo.lstInfo["IndexName"].ErrorType >= DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical ? tableIndexsInfo.lstInfo["IndexName"].ErrorType : DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                    }
                    else
                    {
                        tableIndexsInfo.lstInfo.Add("IndexName", new DevExpress.XtraEditors.DXErrorProvider.ErrorInfo("一个表中不允许存在重复的索引名", DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical));
                    }
                }

                // 判断索引名是否为空
                if (string.IsNullOrEmpty(tableIndexsInfo.IndexName))
                {
                    if (tableIndexsInfo.lstInfo.ContainsKey("IndexName"))
                    {
                        tableIndexsInfo.lstInfo["IndexName"].ErrorText = tableIndexsInfo.lstInfo["IndexName"].ErrorText + "\r\n索引名不能为空";
                        tableIndexsInfo.lstInfo["IndexName"].ErrorType = tableIndexsInfo.lstInfo["IndexName"].ErrorType >= DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical ? tableIndexsInfo.lstInfo["IndexName"].ErrorType : DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                    }
                    else
                    {
                        tableIndexsInfo.lstInfo.Add("IndexName", new DevExpress.XtraEditors.DXErrorProvider.ErrorInfo("索引名不能为空", DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical));
                    }
                }

                // 判断索引字段列表是否为空
                if (string.IsNullOrEmpty(tableIndexsInfo.IndexFieldLst))
                {
                    if (tableIndexsInfo.lstInfo.ContainsKey("IndexFieldLst"))
                    {
                        tableIndexsInfo.lstInfo["IndexFieldLst"].ErrorText = tableIndexsInfo.lstInfo["IndexFieldLst"].ErrorText + "\r\n索引字段列表不能为空";
                        tableIndexsInfo.lstInfo["IndexFieldLst"].ErrorType = tableIndexsInfo.lstInfo["IndexFieldLst"].ErrorType >= DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical ? tableIndexsInfo.lstInfo["IndexFieldLst"].ErrorType : DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                    }
                    else
                    {
                        tableIndexsInfo.lstInfo.Add("IndexFieldLst", new DevExpress.XtraEditors.DXErrorProvider.ErrorInfo("索引字段列表不能为空", DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical));
                    }
                }
            }
        }

    }

    #region SyntaxHighlightService
    public class SyntaxHighlightService : ISyntaxHighlightService
    {
        #region Fields
        readonly DevExpress.XtraRichEdit.RichEditControl editor;
        readonly SyntaxHighlightInfo syntaxHighlightInfo;
        #endregion


        public SyntaxHighlightService(DevExpress.XtraRichEdit.RichEditControl editor)
        {
            this.editor = editor;
            this.syntaxHighlightInfo = new SyntaxHighlightInfo();
        }


        #region ISyntaxHighlightService Members
        public void ForceExecute()
        {
            Execute();
        }
        public void Execute()
        {
            DevExpress.CodeParser.TokenCollection tokens = Parse(editor.Text);
            HighlightSyntax(tokens);
        }
        #endregion
        DevExpress.CodeParser.TokenCollection Parse(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;
            DevExpress.CodeParser.ITokenCategoryHelper tokenizer = CreateTokenizer();
            if (tokenizer == null)
                return new DevExpress.CodeParser.TokenCollection();
            return tokenizer.GetTokens(code);
        }

        DevExpress.CodeParser.ITokenCategoryHelper CreateTokenizer()
        {
            string fileName = editor.Options.DocumentSaveOptions.CurrentFileName;
            if (String.IsNullOrEmpty(fileName))
                return null;
            DevExpress.CodeParser.ITokenCategoryHelper result = DevExpress.CodeParser.TokenCategoryHelperFactory.CreateHelperForFileExtensions(Path.GetExtension(fileName));
            if (result != null)
                return result;
            else
                return null;
        }

        void HighlightSyntax(DevExpress.CodeParser.TokenCollection tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return;
            Document document = editor.Document;
            CharacterProperties cp = document.BeginUpdateCharacters(0, 1);

            List<SyntaxHighlightToken> syntaxTokens = new List<SyntaxHighlightToken>(tokens.Count);
            foreach (DevExpress.CodeParser.Token token in tokens)
            {
                HighlightCategorizedToken((DevExpress.CodeParser.CategorizedToken)token, syntaxTokens);
            }
            document.ApplySyntaxHighlight(syntaxTokens);
            document.EndUpdateCharacters(cp);
        }
        void HighlightCategorizedToken(DevExpress.CodeParser.CategorizedToken token, List<SyntaxHighlightToken> syntaxTokens)
        {
            Color backColor = editor.ActiveView.BackColor;
            SyntaxHighlightProperties highlightProperties = syntaxHighlightInfo.CalculateTokenCategoryHighlight(token.Category);
            SyntaxHighlightToken syntaxToken = SetTokenColor(token, highlightProperties, backColor);
            if (syntaxToken != null)
                syntaxTokens.Add(syntaxToken);
        }
        SyntaxHighlightToken SetTokenColor(DevExpress.CodeParser.Token token, SyntaxHighlightProperties foreColor, Color backColor)
        {
            if (editor.Document.Paragraphs.Count < token.Range.Start.Line)
                return null;
            int paragraphStart = DocumentHelper.GetParagraphStart(editor.Document.Paragraphs[token.Range.Start.Line - 1]);
            int tokenStart = paragraphStart + token.Range.Start.Offset - 1;
            if (token.Range.End.Line != token.Range.Start.Line)
                paragraphStart = DocumentHelper.GetParagraphStart(editor.Document.Paragraphs[token.Range.End.Line - 1]);

            int tokenEnd = paragraphStart + token.Range.End.Offset - 1;
            System.Diagnostics.Debug.Assert(tokenEnd > tokenStart);
            return new SyntaxHighlightToken(tokenStart, tokenEnd - tokenStart, foreColor);
        }
    }
    #endregion

    #region SyntaxHighlightInfo
    public class SyntaxHighlightInfo
    {
        readonly Dictionary<DevExpress.CodeParser.TokenCategory, SyntaxHighlightProperties> properties;

        public SyntaxHighlightInfo()
        {
            this.properties = new Dictionary<DevExpress.CodeParser.TokenCategory, SyntaxHighlightProperties>();
            Reset();
        }
        public void Reset()
        {
            properties.Clear();
            Add(DevExpress.CodeParser.TokenCategory.Text, DXColor.Black);
            Add(DevExpress.CodeParser.TokenCategory.Keyword, DXColor.Blue);
            Add(DevExpress.CodeParser.TokenCategory.String, DXColor.Brown);
            Add(DevExpress.CodeParser.TokenCategory.Comment, DXColor.Green);
            Add(DevExpress.CodeParser.TokenCategory.Identifier, DXColor.Black);
            Add(DevExpress.CodeParser.TokenCategory.PreprocessorKeyword, DXColor.Blue);
            Add(DevExpress.CodeParser.TokenCategory.Number, DXColor.Red);
            Add(DevExpress.CodeParser.TokenCategory.Operator, DXColor.Black);
            Add(DevExpress.CodeParser.TokenCategory.Unknown, DXColor.Black);
            Add(DevExpress.CodeParser.TokenCategory.XmlComment, DXColor.Gray);

            Add(DevExpress.CodeParser.TokenCategory.CssComment, DXColor.Green);
            Add(DevExpress.CodeParser.TokenCategory.CssKeyword, DXColor.Brown);
            Add(DevExpress.CodeParser.TokenCategory.CssPropertyName, DXColor.Red);
            Add(DevExpress.CodeParser.TokenCategory.CssPropertyValue, DXColor.Blue);
            Add(DevExpress.CodeParser.TokenCategory.CssSelector, DXColor.Blue);
            Add(DevExpress.CodeParser.TokenCategory.CssStringValue, DXColor.Blue);

            Add(DevExpress.CodeParser.TokenCategory.HtmlAttributeName, DXColor.Red);
            Add(DevExpress.CodeParser.TokenCategory.HtmlAttributeValue, DXColor.Blue);
            Add(DevExpress.CodeParser.TokenCategory.HtmlComment, DXColor.Green);
            Add(DevExpress.CodeParser.TokenCategory.HtmlElementName, DXColor.Brown);
            Add(DevExpress.CodeParser.TokenCategory.HtmlEntity, DXColor.Gray);
            Add(DevExpress.CodeParser.TokenCategory.HtmlOperator, DXColor.Black);
            Add(DevExpress.CodeParser.TokenCategory.HtmlServerSideScript, DXColor.Black);
            Add(DevExpress.CodeParser.TokenCategory.HtmlString, DXColor.Blue);
            Add(DevExpress.CodeParser.TokenCategory.HtmlTagDelimiter, DXColor.Blue);
        }
        void Add(DevExpress.CodeParser.TokenCategory category, Color foreColor)
        {
            SyntaxHighlightProperties item = new SyntaxHighlightProperties();
            item.ForeColor = foreColor;
            properties.Add(category, item);
        }

        public SyntaxHighlightProperties CalculateTokenCategoryHighlight(DevExpress.CodeParser.TokenCategory category)
        {
            SyntaxHighlightProperties result = null;
            if (properties.TryGetValue(category, out result))
                return result;
            else
                return properties[DevExpress.CodeParser.TokenCategory.Text];
        }
    }
    #endregion

    #region CustomRichEditCommandFactoryService
    public class CustomRichEditCommandFactoryService : IRichEditCommandFactoryService
    {
        readonly IRichEditCommandFactoryService service;

        public CustomRichEditCommandFactoryService(IRichEditCommandFactoryService service)
        {
            Guard.ArgumentNotNull(service, "service");
            this.service = service;
        }

        #region IRichEditCommandFactoryService Members
        RichEditCommand IRichEditCommandFactoryService.CreateCommand(RichEditCommandId id)
        {
            if (id.Equals(RichEditCommandId.InsertColumnBreak) || id.Equals(RichEditCommandId.InsertLineBreak) || id.Equals(RichEditCommandId.InsertPageBreak))
                return service.CreateCommand(RichEditCommandId.InsertParagraph);
            return service.CreateCommand(id);
        }
        #endregion
    }
    #endregion

    public static class SourceCodeDocumentFormat
    {
        public static readonly DocumentFormat Id = new DocumentFormat(1325);
    }
    public class SourcesCodeDocumentImporter : PlainTextDocumentImporter
    {
        internal static readonly FileDialogFilter filter = new FileDialogFilter("Source Files", new string[] { "cs", "vb", "html", "htm", "js", "xml", "css" });
        public override FileDialogFilter Filter { get { return filter; } }
        public override DocumentFormat Format { get { return SourceCodeDocumentFormat.Id; } }
    }
    public class SourcesCodeDocumentExporter : PlainTextDocumentExporter
    {
        public override FileDialogFilter Filter { get { return SourcesCodeDocumentImporter.filter; } }
        public override DocumentFormat Format { get { return SourceCodeDocumentFormat.Id; } }
    }
}
