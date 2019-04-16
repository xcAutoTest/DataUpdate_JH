using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DataUpdate
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }
        private DataTable dt_AlreadyUpload = null;//已上传表
        private DataTable dt_UploadFailed = null;//已上传表
        private DataTable dt_ReadyUpload = null;//待上传、上传失败表
        private DataTable dt_ZjBdUpload = null;
        private DBControl carinfodb = new DBControl();//定义数据库操作
        private Interface InterfaceUpload = null;//定义联网操作
        private interfaceJinhua interfaceUploadJh = null;
        private bool _auto_upload = false;//初始定义上传模式为手动
        private bool _whether_Connected = false;//定义联网是否可以正常工作
        private bool _clearcarlistat500 = true;//用于判断程序是否在6点左右执行了一次定时清理待检列表
        private int Choosedindex = 0;//手动上传时被选中行的行号
        private Dictionary<string, string> JcZt = new Dictionary<string, string>();

        #region 更新上传过程状态表显示
        public delegate void udgv(string jylsh, string jycs, string column_name,  string zt);
        /// <summary>
        /// 更新软件界面待上传车辆显示状态
        /// </summary>
        /// <param name="jylsh">检验流水号</param>
        /// <param name="jycs">检验次数</param>
        /// <param name="column_name">列名</param>
        /// <param name="zt">状态</param>
        public void ShowStatus(string jylsh, string jycs, string column_name, string zt)
        {
            try
            {
                if (dt_ReadyUpload != null)
                {
                    DataRow[] temp_dr = dt_ReadyUpload.Select("检验流水号 = '" + jylsh + "' and 检验次数 = '" + jycs + "'");
                    if (temp_dr.Count() > 0)
                    {
                        dt_ReadyUpload.Rows[dt_ReadyUpload.Rows.IndexOf(temp_dr[0])][column_name] = zt;
                    }
                }
            }
            catch { }
        }
        //用于线程中更新检测状态到备注
        public void UpdateStatus(string jylsh, string jycs, string column_name, string zt)
        {
            try
            {
                BeginInvoke(new udgv(ShowStatus), jylsh, jycs, column_name, zt);
            }
            catch { }
        }
        #endregion
        
        private void FrmMain_Load(object sender, EventArgs e)
        {
            #region 获取配置信息
            if (FileOpreate.GetDBConfig() == false)
            {
                MessageBox.Show("获取数据库配置信息失败！");
                return;
            }
            if (FileOpreate.GetInterfaceconfig() == false)
            {
                MessageBox.Show("获取联网配置信息失败！");
                return;
            }
            ModPublicJHHB.webAddress = FileOpreate.Interface_Address;
            ModPublicJHHB.jksqm = FileOpreate.Interface_Jkxlh;
            ModPublicJHHB.TsNo = FileOpreate.Interface_JczID;
            if (FileOpreate.GetSoftconfig() == false)
            {
                MessageBox.Show("获取软件配置信息失败！");
                return;
            }
            if (FileOpreate.GetUserconfig() == false)
            {
                MessageBox.Show("登录初始化失败！");
                return;
            }
            #endregion

            #region 功能测试区

            #endregion

            #region 显示登录界面
            LoginFrom loginfrom = new LoginFrom();
            loginfrom.ShowDialog();
            if (loginfrom.LoginStatus == false)
            {
                this.Close();
                return;
            }

            FileOpreate.SaveLog("软件登录成功", "当前状态", 1);
            #endregion

            #region 控件初始位置设置
            dataGridView_AlreadyUpload.Location = new Point(12, 28);
            dataGridView_Ready_And_Failed.Location = new Point(12, 28);
            dataGridView_BDZJ.Location = new Point(12, 28);
            dataGridView_UploadFailed.Location = new Point(12, 28);
            #endregion

            #region 软件界面控件初始化
            _auto_upload = FileOpreate.UpdateStyle == 1 ? true : false;//数据上传方式，自动还是手动
            button_UpdateByMan.Visible = FileOpreate.UpdateFailSovle == 1 ? true : false;//上传失败处理方式，是否允许手动再次上传
            timerGetWaitCarList.Interval = FileOpreate.UpdateGetWaitListTime * 1000;//设置获取待检列表时间间隔

            #region 已上传表初始化并绑定数据源
            dt_AlreadyUpload = new DataTable();
            dt_AlreadyUpload.Columns.Add("检验流水号");
            dt_AlreadyUpload.Columns.Add("车辆号牌");
            dt_AlreadyUpload.Columns.Add("号牌种类");
            dt_AlreadyUpload.Columns.Add("线号");
            dt_AlreadyUpload.Columns.Add("检验次数");
            dt_AlreadyUpload.Columns.Add("检验时间");
            dt_AlreadyUpload.Columns.Add("检测方法");
            dt_AlreadyUpload.Columns.Add("上传成功时间");
            dataGridView_AlreadyUpload.DataSource = dt_AlreadyUpload;
            dataGridView_AlreadyUpload.Columns["检验流水号"].Width = 160;
            dataGridView_AlreadyUpload.Columns["车辆号牌"].Width = 100;
            dataGridView_AlreadyUpload.Columns["号牌种类"].Width = 100;
            dataGridView_AlreadyUpload.Columns["线号"].Width = 80;
            dataGridView_AlreadyUpload.Columns["检验次数"].Width = 80;
            dataGridView_AlreadyUpload.Columns["检验时间"].Width = 160;
            dataGridView_AlreadyUpload.Columns["检测方法"].Width = 100;
            dataGridView_AlreadyUpload.Columns["上传成功时间"].Width = 160;
            #endregion

            #region 待上传表初始化并绑定数据源
            dt_ReadyUpload = new DataTable();
            dt_ReadyUpload.Columns.Add("检验流水号");
            dt_ReadyUpload.Columns.Add("车辆号牌");
            dt_ReadyUpload.Columns.Add("号牌种类");
            dt_ReadyUpload.Columns.Add("线号");
            dt_ReadyUpload.Columns.Add("检验次数");
            dt_ReadyUpload.Columns.Add("检验时间");
            dt_ReadyUpload.Columns.Add("检测方法");
            dt_ReadyUpload.Columns.Add("当前状态");
            dt_ReadyUpload.Columns.Add("已处理状态");
            dt_ReadyUpload.Columns.Add("备注");
            dataGridView_Ready_And_Failed.DataSource = dt_ReadyUpload;
            dataGridView_Ready_And_Failed.Columns["检验流水号"].Width = 100;
            dataGridView_Ready_And_Failed.Columns["车辆号牌"].Width = 100;
            dataGridView_Ready_And_Failed.Columns["号牌种类"].Width = 80;
            dataGridView_Ready_And_Failed.Columns["线号"].Width = 60;
            dataGridView_Ready_And_Failed.Columns["检验次数"].Width = 80;
            dataGridView_Ready_And_Failed.Columns["检验时间"].Width = 100;
            dataGridView_Ready_And_Failed.Columns["检测方法"].Width = 80;
            dataGridView_Ready_And_Failed.Columns["当前状态"].Width = 80;
            dataGridView_Ready_And_Failed.Columns["已处理状态"].Width = 100;
            dataGridView_Ready_And_Failed.Columns["备注"].Width = 100;
            #endregion
            #region 上传失败表初始化并绑定数据源
            dt_UploadFailed = new DataTable();
            dt_ReadyUpload.Columns.Add("检验流水号");
            dt_ReadyUpload.Columns.Add("车辆号牌");
            dt_ReadyUpload.Columns.Add("号牌种类");
            dt_ReadyUpload.Columns.Add("线号");
            dt_ReadyUpload.Columns.Add("检验次数");
            dt_ReadyUpload.Columns.Add("检验时间");
            dt_ReadyUpload.Columns.Add("检测方法");
            dt_ReadyUpload.Columns.Add("当前状态");
            dt_ReadyUpload.Columns.Add("已处理状态");
            dt_ReadyUpload.Columns.Add("备注");
            dataGridView_UploadFailed.DataSource = dt_UploadFailed;
            dataGridView_UploadFailed.Columns["检验流水号"].Width = 100;
            dataGridView_UploadFailed.Columns["车辆号牌"].Width = 100;
            dataGridView_UploadFailed.Columns["号牌种类"].Width = 80;
            dataGridView_UploadFailed.Columns["线号"].Width = 60;
            dataGridView_UploadFailed.Columns["检验次数"].Width = 80;
            dataGridView_UploadFailed.Columns["检验时间"].Width = 100;
            dataGridView_UploadFailed.Columns["检测方法"].Width = 80;
            dataGridView_UploadFailed.Columns["当前状态"].Width = 80;
            dataGridView_UploadFailed.Columns["已处理状态"].Width = 100;
            dataGridView_UploadFailed.Columns["备注"].Width = 100;
            #endregion
            #region 自检标定记录表初始化
            dt_ZjBdUpload = new DataTable();
            dt_ZjBdUpload.Columns.Add("线号");
            dt_ZjBdUpload.Columns.Add("Data1");
            dt_ZjBdUpload.Columns.Add("Data2");
            dt_ZjBdUpload.Columns.Add("Data3");
            dt_ZjBdUpload.Columns.Add("Data4");
            dt_ZjBdUpload.Columns.Add("Data5");
            dt_ZjBdUpload.Columns.Add("Data6");
            dt_ZjBdUpload.Columns.Add("Data7");
            dt_ZjBdUpload.Columns.Add("Data8");
            dt_ZjBdUpload.Columns.Add("Data9");
            dt_ZjBdUpload.Columns.Add("Data10");
            dt_ZjBdUpload.Columns.Add("Data11");
            dt_ZjBdUpload.Columns.Add("Data12");
            dt_ZjBdUpload.Columns.Add("Data13");
            dt_ZjBdUpload.Columns.Add("Data14");
            dt_ZjBdUpload.Columns.Add("Data15");

            DataTable line_inf = carinfodb.getLineInfo();
            if (line_inf != null && line_inf.Rows.Count > 0)
            {
                for (int i = 0; i < line_inf.Rows.Count; i++)
                {
                    DataRow dr_new = dt_ZjBdUpload.NewRow();
                    dr_new["线号"] = line_inf.Rows[i]["LINEID"].ToString();
                    dr_new["Data1"] = "未上传";
                    dr_new["Data2"] = "未上传";
                    dr_new["Data3"] = "未上传";
                    dr_new["Data4"] = "未上传";
                    dr_new["Data5"] = "未上传";
                    dr_new["Data6"] = "未上传";
                    dr_new["Data7"] = "未上传";
                    dr_new["Data8"] = "未上传";
                    dr_new["Data9"] = "未上传";
                    dr_new["Data10"] = "未上传";
                    dr_new["Data11"] = "未上传";
                    dr_new["Data12"] = "未上传";
                    dr_new["Data13"] = "未上传";
                    dr_new["Data14"] = "未上传";
                    dr_new["Data15"] = "未上传";
                    dt_ZjBdUpload.Rows.Add(dr_new);
                }
            }
            else
            {
                MessageBox.Show("获取检测线配置信息失败，请检测设置后重试！");
                return;
            }
            #endregion

            //初始化检测状态
            InitJcZt();
            #endregion

            #region 联网初始化
            if (FileOpreate.Interface_Address == "" || FileOpreate.Interface_JczID == "" || FileOpreate.Interface_Jkxlh == "")
            {
                MessageBox.Show("联网接口配置信息不全，请完善接口配置后重试！");
                return;
            }
            else
            {
                #region 获取时间查询表方式（废弃）
                /*
                try
                {
                    InterfaceUpload = new Interface(FileOpreate.Interface_Address, FileOpreate.Interface_Jkxlh, FileOpreate.Interface_JczID);//接口初始化
                    DataTable dttime = InterfaceUpload.GetSystemDatetime();//获取平台时间
                    if (dttime != null)
                    {
                        DateTime systemtime = DateTime.Parse(dttime.Rows[0]["syndate"].ToString());
                        SetSystemDateTime.SetLocalTimeByStr(systemtime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        _whether_Connected = true;
                        if (_auto_upload == true)//自动上传
                        {
                            timer_UploadInfo.Enabled = true;
                            开始自动上传ToolStripMenuItem.Enabled = false;
                            关闭自动上传ToolStripMenuItem.Enabled = true;
                        }
                        timer_GetWaitCarList.Enabled = true;//定时获取待检列表
                    }
                    else
                    {
                        _whether_Connected = false;
                        FileOpreate.SaveLog("时间同步失败", "InterfaceError2", 1);
                        MessageBox.Show("时间同步失败！");
                    }
                }*/
                #endregion

                #region 直接返回时间查询成功与否及时间结果
                try
                {
                    interfaceUploadJh = new interfaceJinhua();
                    //InterfaceUpload = new Interface(FileOpreate.Interface_Address, FileOpreate.Interface_Jkxlh, FileOpreate.Interface_JczID);//接口初始化
                    FileOpreate.SaveLog("InterfaceInfo_Address:" + FileOpreate.Interface_Address + " | Jkxlh:" + FileOpreate.Interface_Jkxlh + " | JczID:" + FileOpreate.Interface_JczID + ",接口初始化成功", "接口初始化", 1);
                    DateTime server_time;//获取平台时间
                    if (interfaceUploadJh.synSystemTime(out server_time))
                    {
                        if (SetSystemDateTime.SetLocalTimeByStr(server_time.ToString("yyyy-MM-dd HH:mm:ss.fff")))
                        {
                            string temp_status = "同步平台时间成功，当前时间："+ server_time.ToString();
                             _whether_Connected = true;

                            /*金华暂时不获取列表，由康总的外检服务程序获取列表
                            if (FileOpreate.UpdateAutoGetCarList == 1)//自动定时获取待检列表
                            {
                                timerGetWaitCarList.Enabled = true;
                                timerGetWaitCarList.Start();
                                temp_status = temp_status + " | 自动获取待检列表，时间间隔：" + FileOpreate.UpdateGetWaitListTime.ToString() + "s";
                            }*/
                            if (_auto_upload)//自动上传
                            {
                                timerBdZjUpdate.Enabled = true;//标定自检数据自动上传定时器
                                timerUploadTestData.Enabled = true;//检测数据自动上传定时器
                                timerRefreshFaileData.Enabled = true;
                                开始自动上传ToolStripMenuItem.Enabled = false;
                                关闭自动上传ToolStripMenuItem.Enabled = true;
                                temp_status = temp_status + " | 数据自动上传开启，请先完成各线的标定自检操作，程序将自动上传标定自检记录！";
                                //timerBdZjUpdate.Start();
                            }
                            else
                                temp_status = temp_status + " | 请选择一条数据进行手动上传";
                            tSSL_Status.Text = temp_status;
                        }
                        else
                        {
                            tSSL_Status.Text = "获取平台时间成功，但同步本机时间失败";
                            FileOpreate.SaveLog("更新系统时间失败！", "时间同步", 1);
                            return;
                        }
                    }
                    else
                    {
                        _whether_Connected = false;
                        FileOpreate.SaveLog("获取平台时间失败！", "时间同步", 1);
                        MessageBox.Show("获取平台时间失败！");
                        return;
                    }
                }
                catch (Exception er)
                {
                    _whether_Connected = false;
                    FileOpreate.SaveLog("联网接口初始化错误，原因：" + er.Message, "接口初始化", 1);
                    MessageBox.Show("联网接口初始化错误，错误原因：" + er.Message);
                    return;
                }
                #endregion
            }
            #endregion

            #region 当天第一次启动时执行一次待检列表清理
            /*
            try
            {
                DateTime begin_date = FileOpreate.GetClearTime();
                if (DateTime.Compare(DateTime.Now.Date, begin_date.Date) > 0)//软件上次启动时间为今天之前时清理待检列表并保存第一次启动时间
                {
                    carinfodb.clearCarWaitList();
                    carinfodb.clearCarTestStatus();
                    FileOpreate.SaveClearTime(System.DateTime.Now.ToString("yyyy-MM-dd"));
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("执行软件当天第一次启动时清理待检列表及车辆检测表失败，原因：" + er.Message, "定时清理", 1);
                tSSL_Status.Text = "软件启动状态：联网接口初始成功，但未能自动清理待检列表及车辆检测状态表，原因："+ er.Message;
            }
            */
            #endregion
            
           // timerUpdateInterface.Enabled = true;//开启界面刷新
        }

        #region 软件设置各按钮
        //窗口大小变化时强制重绘窗体
        private void FrmMain_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
        }
        
        private void 开始自动上传ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_whether_Connected == true)
            {
                timerUploadTestData.Enabled = true;
                timerRefreshFaileData.Enabled = true;
                开始自动上传ToolStripMenuItem.Enabled = false;
                关闭自动上传ToolStripMenuItem.Enabled = true;
                tSSL_Status.Text = "状态：已开启数据自动上传！";
            }
            else
                MessageBox.Show("请先正常联网并上传标定和自检数据后再试！");
        }

        private void 关闭自动上传ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timerUploadTestData.Enabled = false;
            timerRefreshFaileData.Enabled = false;
            开始自动上传ToolStripMenuItem.Enabled = true;
            tSSL_Status.Text = "状态：数据自动上传关闭！";
        }

        private void 待上传数据ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView_AlreadyUpload.Visible = false;
            dataGridView_BDZJ.Visible = false;
            dataGridView_Ready_And_Failed.Visible = true;
            tSSL_Status.Text = "状态：当前显示正在上传或上传失败数据列表！";
        }

        private void 已上传数据ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView_AlreadyUpload.Visible = true;
            dataGridView_Ready_And_Failed.Visible = false;
            tSSL_Status.Text = "状态：当前显示已上传数据列表！";
        }

        //手动上传按钮
        private void button_UpdateByMan_Click(object sender, EventArgs e)
        {
            if (!_whether_Connected)//若同步时间失败则不上传
            {
                MessageBox.Show("接口未正常初始化，不能手动上传！");
                return;
            }
            
            try
            {
                string jylsh = dt_ReadyUpload.Rows[Choosedindex]["检验流水号"].ToString();
                string jycs = dt_ReadyUpload.Rows[Choosedindex]["检验次数"].ToString();
                string lineid= dt_ReadyUpload.Rows[Choosedindex]["线号"].ToString();
                DataRow CarTestStatusRow = carinfodb.getCarTestStatusByLsh(jylsh, jycs);
                
                CarTestStatusRow["HPZL"] = InterfaceUpload.get_hpzl_code[CarTestStatusRow["HPZL"].ToString()];
                if (CarTestStatusRow != null)
                {
                    string tempZT = CarTestStatusRow["ZT"].ToString();
                    string tempYCLZT = CarTestStatusRow["YCLZT"].ToString();
                    
                    //3为联网验证未通过，5为检测未完成，当前状态不为1且当前状态等于已处理状态，以上不执行
                    //其他都要执行（不能正常执行的重新开始检测后zt会自动更新为1）
                    if (tempZT == "3" || tempZT == "5" || tempZT == tempYCLZT)
                        return;
                    //正常开始之后关闭检查软件导致ZT与YCLZT相同的还原
                    if (tempZT == "1" && tempYCLZT == "1")
                        carinfodb.UpdateCarTestStatusYCLZT("0", jylsh, jycs);
                    try
                    {
                        switch (CarTestStatusRow["JCFF"].ToString())
                        {
                            case "ASM":
                                if (tempZT == "1" || tempZT == "21" || tempZT == "22" || tempZT == "23" || tempZT == "24" || tempZT == "6")
                                {
                                    Thread AsmUploadThread = new Thread(new ParameterizedThreadStart(UploadASMData));//新建一个过程的上传线程
                                    AsmUploadThread.Start(CarTestStatusRow);//开始上传线程
                                    //carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);
                                    ShowStatus(jylsh, jycs, "备注", "ASM手动上传中...");
                                    FileOpreate.SaveLog("ASM数据手动上传线程已开始", "检测数据上传", 1);
                                }
                                break;
                            case "VMAS":
                                if (tempZT == "1" || tempZT == "31" || tempZT == "32" || tempZT == "33" || tempZT == "6")
                                {
                                    Thread VmasUploadThread = new Thread(new ParameterizedThreadStart(UploadVMASData));
                                    VmasUploadThread.Start(CarTestStatusRow);
                                    //carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);
                                    ShowStatus(jylsh, jycs, "备注", "VMAS手动上传中...");
                                    FileOpreate.SaveLog("VMAS数据手动上传线程已开始", "检测数据上传", 1);
                                }
                                break;
                            case "SDS":
                                if (tempZT == "1" || tempZT == "11" || tempZT == "12" || tempZT == "6")
                                {
                                    Thread SdsUploadThread = new Thread(new ParameterizedThreadStart(UploadSDSData));
                                    SdsUploadThread.Start(CarTestStatusRow);
                                    //carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);
                                    ShowStatus(jylsh, jycs, "备注", "SDS手动上传中...");
                                    FileOpreate.SaveLog("SDS数据手动上传线程已开始", "检测数据上传", 1);
                                }
                                break;
                            case "SDSM":
                                if (tempZT == "1" || tempZT == "81" || tempZT == "82" || tempZT == "6")
                                {
                                    Thread SdsMUploadThread = new Thread(new ParameterizedThreadStart(UploadSDSMData));
                                    SdsMUploadThread.Start(CarTestStatusRow);
                                    //carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);
                                    ShowStatus(jylsh, jycs, "备注", "SDSM手动上传中...");
                                    FileOpreate.SaveLog("SDSM数据手动上传线程已开始", "检测数据上传", 1);
                                }
                                break;
                            case "JZJS":
                                if (tempZT == "1" || tempZT == "41" || tempZT == "42" || tempZT == "43" || tempZT == "6")
                                {
                                    Thread JzjsUploadThread = new Thread(new ParameterizedThreadStart(UploadJZJSData));
                                    JzjsUploadThread.Start(CarTestStatusRow);
                                    //carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);
                                    ShowStatus(jylsh, jycs, "备注", "JZJS手动上传中...");
                                    FileOpreate.SaveLog("JZJS数据手动上传线程已开始", "检测数据上传", 1);
                                }
                                break;
                            case "ZYJS":
                                if (tempZT == "1" || tempZT == "51" || tempZT == "52" || tempZT == "53" || tempZT == "6")
                                {
                                    Thread ZyjsUploadThread = new Thread(new ParameterizedThreadStart(UploadZYJSData));
                                    ZyjsUploadThread.Start(CarTestStatusRow);
                                    //carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);
                                    ShowStatus(jylsh, jycs, "备注", "ZYJS手动上传中...");
                                    FileOpreate.SaveLog("ZYJS数据手动上传线程已开始", "检测数据上传", 1);
                                }
                                break;
                        }
                    }
                    catch (Exception er)
                    {
                        FileOpreate.SaveLog("开启线程失败，原因：" + er.Message, "检测数据上传", 1);
                    }
                    button_UpdateByMan.Enabled = false;
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("失败原因：" + er.Message, "手动上传错误", 1);
                MessageBox.Show(er.Message, "手动上传错误");
            }
        }

        //删除选中项目
        private void 删除上传失败记录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView_Ready_And_Failed.Visible == true && MessageBox.Show("您确定要删除该行数据吗？", "请注意", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                try
                {
                    string jylsh = dt_ReadyUpload.Rows[Choosedindex]["检验流水号"].ToString();
                    string jycs = dt_ReadyUpload.Rows[Choosedindex]["检验次数"].ToString();

                    //待检列表、检测状态表、上传软件界面都删一次操作
                    string strDeleteResult = "状态：流水号" + jylsh + "，检验次数" + jycs;
                    if (carinfodb.deleteCarInWaitlist(jylsh, jycs))//删除本地待检列表
                        strDeleteResult += "本地待检列表删除成功|";
                    else
                        strDeleteResult += "本地待检列表删除失败|";
                    if (carinfodb.DeleteCarTestStatus(jylsh, jycs))//删除车辆检测状态表
                        strDeleteResult += "本地车辆检测状态表删除成功|";
                    else
                        strDeleteResult += "本地车辆检测状态表删除失败|";
                    DataRow[] ReadyToDelete = dt_ReadyUpload.Select("检验流水号 = '" + jylsh + "' and 检验次数 = '" + jycs + "'");
                    if (ReadyToDelete.Count() > 0)//软件界面有这个检验流水号及检验次数的记录就删除
                    {
                        for (int j = 0; j < ReadyToDelete.Count(); j++)
                        {
                            dt_ReadyUpload.Rows.Remove(ReadyToDelete[j]);
                        }
                        strDeleteResult += "上传软件界面记录删除成功";
                    }
                    strDeleteResult += "上传软件界面无记录";
                    tSSL_Status.Text = strDeleteResult;
                    FileOpreate.SaveLog(strDeleteResult, "待检车辆信息删除", 1);
                }
                catch (Exception er)
                {
                    tSSL_Status.Text = "数据删除错误：该行数据删除失败，原因：" + er.Message;
                    FileOpreate.SaveLog("删除失败记录失败，原因：" + er.Message, "主界面按钮", 1);
                }
            }
        }

        //选中行数发生变化时修改设置的行号
        private void dataGridView_Ready_And_Failed_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            Choosedindex = e.RowIndex;
            button_UpdateByMan.Enabled = true;
        }

        //查询车辆数据上传状态
        private void button_SeachByCarNum_Click(object sender, EventArgs e)
        {
            try
            {
                //设置选中行dataGridView_Ready_And_Failed.Rows[1].Selected = true;
                if (dataGridView_Ready_And_Failed.Visible == true && dt_ReadyUpload != null)
                {
                    foreach (DataGridViewRow dgvr in dataGridView_Ready_And_Failed.Rows)
                    {
                        dgvr.Selected = false;
                    }
                    DataRow[] temp_dr = null;
                    int a = 0;
                    if (int.TryParse(textBox_CarNum.Text, out a))
                    {
                        temp_dr = dt_ReadyUpload.Select("车辆号牌 like '%" + a + "%'");
                    }
                    else if (textBox_CarNum.Text.Length <= 7 && !System.Text.RegularExpressions.Regex.IsMatch(textBox_CarNum.Text, "[\u4e00-\u9fbb]"))
                    {
                        temp_dr = dt_ReadyUpload.Select("车辆号牌 like '%" + textBox_CarNum.Text + "%'");
                    }
                    else if (textBox_CarNum.Text.Length == 7 || textBox_CarNum.Text.Length == 8)
                    {
                        temp_dr = dt_ReadyUpload.Select("车辆号牌 = '" + textBox_CarNum.Text + "'");
                    }
                    else
                    {
                        MessageBox.Show("输入车牌格式不对，请正确输入车牌号或直接输入英文加数字部分！");
                        return;
                    }
                    if (temp_dr.Count() > 0)
                    {
                        dataGridView_Ready_And_Failed.Rows[dt_ReadyUpload.Rows.IndexOf(temp_dr[0])].Selected = true;
                        dataGridView_Ready_And_Failed.CurrentCell = dataGridView_Ready_And_Failed.Rows[dt_ReadyUpload.Rows.IndexOf(temp_dr[0])].Cells[1];
                    }
                    else
                        MessageBox.Show("待上传或上传失败数据中未找到相应车辆信息");
                }
                else if (dataGridView_AlreadyUpload.Visible == true)
                {
                    foreach (DataGridViewRow dgvr in dataGridView_AlreadyUpload.Rows)
                    {
                        dgvr.Selected = false;
                    }
                    DataRow[] temp_dr = null;
                    int a = 0;
                    if (int.TryParse(textBox_CarNum.Text, out a))
                    {
                        temp_dr = dt_AlreadyUpload.Select("车辆号牌 like '%" + a + "%'");
                    }
                    else if (textBox_CarNum.Text.Length <= 7 && !System.Text.RegularExpressions.Regex.IsMatch(textBox_CarNum.Text, "[\u4e00-\u9fbb]"))
                    {
                        temp_dr = dt_AlreadyUpload.Select("车辆号牌 like '%" + textBox_CarNum.Text + "%'");
                    }
                    else if (textBox_CarNum.Text.Length == 7 || textBox_CarNum.Text.Length == 8)
                    {
                        temp_dr = dt_AlreadyUpload.Select("车辆号牌 = '" + textBox_CarNum.Text + "'");
                    }
                    else
                    {
                        MessageBox.Show("输入车牌格式不对，请正确输入车牌号或直接输入英文加数字部分！");
                        return;
                    }
                    if (temp_dr.Count() > 0)
                    {
                        dataGridView_AlreadyUpload.Rows[dt_AlreadyUpload.Rows.IndexOf(temp_dr[0])].Selected = true;
                        dataGridView_AlreadyUpload.CurrentCell = dataGridView_AlreadyUpload.Rows[dt_AlreadyUpload.Rows.IndexOf(temp_dr[0])].Cells[1];
                    }
                    else
                        MessageBox.Show("已上传数据中未找到相应车辆信息");
                }
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message, "上传记录查询失败");
            }
        }

        private void 上传自检数据ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable SelfCheckDataTable = carinfodb.getSelfCheckData(maxUploadTimes);
                if (SelfCheckDataTable != null && SelfCheckDataTable.Rows.Count > 0)
                {
                    FileOpreate.SaveLog("手动开始进行自检数据上传，总共有：" + SelfCheckDataTable.Rows.Count + "行数据！", "自检数据上传", 1);
                    for (int i = 0; i < SelfCheckDataTable.Rows.Count; i++)
                    {

                        UpdateZj(SelfCheckDataTable.Rows[i]);
                        /*
                        Thread JZHXSelfCheckUploadThread = new Thread(new ParameterizedThreadStart(UpdateZj));//新建一个标定数据上传线程
                        JZHXSelfCheckUploadThread.Start(SelfCheckDataTable.Rows[i]);//开始上传线程
                        */

                    }
                }
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message, "自检数据手动上传失败原因");
            }
        }
        private int maxUploadTimes = 3;
        private void 上传标定数据ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable BDDataTable = carinfodb.getBDData(maxUploadTimes);//获取当天未上传标定数据
                if (BDDataTable != null && BDDataTable.Rows.Count > 0)
                {
                    FileOpreate.SaveLog("手动开始进行标定数据上传，总共有：" + BDDataTable.Rows.Count + "行数据！", "标定数据上传", 1);
                    for (int i = 0; i < BDDataTable.Rows.Count; i++)
                    {
                        //object obj = (object)(BDDataTable.Rows[i]);
                        /*
                        Thread SpeedBDUploadThread = new Thread(new ParameterizedThreadStart(UpdateBD));//新建一个标定数据上传线程
                        SpeedBDUploadThread.Start(BDDataTable.Rows[i]);//开始上传线程*/
                        UpdateBD(BDDataTable.Rows[i]);//不放在进程里面，以免上传时间长重复进入上传进程
                    }
                }
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message, "标定数据手动上传失败原因");
            }
        }
        #endregion

        #region 定时器：获取待检列表定时器、自动上传定时器、界面刷新定时器
        //自动上传定时器
        private void timerBdZjUpdate_Tick(object sender, EventArgs e)
        {
            #region 自检数据上传
            try
            {
                DataTable SelfCheckDataTable = carinfodb.getSelfCheckData(maxUploadTimes);
                if (SelfCheckDataTable != null && SelfCheckDataTable.Rows.Count > 0)
                {
                    FileOpreate.SaveLog("自动开始进行自检数据上传，总共有：" + SelfCheckDataTable.Rows.Count + "行数据！", "自检数据上传", 1);
                    for (int i = 0; i < SelfCheckDataTable.Rows.Count; i++)
                    {
                        UpdateZj(SelfCheckDataTable.Rows[i]);
                        /*
                        Thread JZHXSelfCheckUploadThread = new Thread(new ParameterizedThreadStart(UpdateZj));//新建一个标定数据上传线程
                        JZHXSelfCheckUploadThread.Start(SelfCheckDataTable.Rows[i]);//开始上传线程
                        */

                    }
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("错误原因：" + er.Message, "设备自检数据上传错误", 1);
            }
            try
            {
                DataTable BDDataTable = carinfodb.getBDData(maxUploadTimes);//获取当天未上传标定数据
                if (BDDataTable != null && BDDataTable.Rows.Count > 0)
                {
                    FileOpreate.SaveLog("手动开始进行标定数据上传，总共有：" + BDDataTable.Rows.Count + "行数据！", "标定数据上传", 1);
                    for (int i = 0; i < BDDataTable.Rows.Count; i++)
                    {
                        //object obj = (object)(BDDataTable.Rows[i]);
                        /*
                        Thread SpeedBDUploadThread = new Thread(new ParameterizedThreadStart(UpdateBD));//新建一个标定数据上传线程
                        SpeedBDUploadThread.Start(BDDataTable.Rows[i]);//开始上传线程*/
                        UpdateBD(BDDataTable.Rows[i]);//不放在进程里面，以免上传时间长重复进入上传进程
                    }
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("错误原因：" + er.Message, "设备标定数据上传错误", 1);
            }
            #endregion

        }

        // 待检列表获取
        private void timerGetWaitCarList_Tick(object sender, EventArgs e)
        {
            #region 连续不间断运行时在05：00-05：30间进行一次定时清理待检列表
            try
            {
                if (_clearcarlistat500 && DateTime.Now.CompareTo(Convert.ToDateTime("05:30")) < 0 && DateTime.Now.CompareTo(Convert.ToDateTime("05:00")) > 0)
                {
                    //在5:00到5:30之间，进行一次清理，并将_clearcarlistat500状态置为fales，保存清理时间
                    carinfodb.clearCarWaitList();
                    carinfodb.clearCarTestStatus();
                    _clearcarlistat500 = false;
                    FileOpreate.SaveClearTime(System.DateTime.Now.ToString("yyyy-MM-dd"));

                    //重置自检标定上传记录
                    if (dt_ZjBdUpload != null && dt_ZjBdUpload.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt_ZjBdUpload.Rows.Count; i++)
                        {
                            for (int j = 1; j < 16; j++)
                            {
                                dt_ZjBdUpload.Rows[i][j] = "未上传";
                            }
                        }
                    }
                }
                if (!_clearcarlistat500 && DateTime.Now.CompareTo(Convert.ToDateTime("05:30")) > 0)
                    _clearcarlistat500 = true;
            }
            catch {}
            #endregion
            
            #region 获取平台待检列表
            try
            {
                string error_info1 = "";
                DataTable dtWaitCarListInNet = InterfaceUpload.GetVehicleList(out error_info1);//联网获取待检车辆信息
                if (dtWaitCarListInNet == null)
                {
                    FileOpreate.SaveLog("获取错误,原因：" + error_info1, "获取平台待检列表", 1);
                    return;
                }
                if (dtWaitCarListInNet.Rows.Count > 0)
                {
                    DataTable dtWaitCarList = carinfodb.getCarInWaitList("Y");//获取待检列表中的联网待检车辆信息
                    if (dtWaitCarList == null)
                    {
                        FileOpreate.SaveLog("获取错误,原因：" + error_info1, "获取本地待检列表", 1);
                        return;
                    }
                    
                    if (FileOpreate.Interface_Area == 0)
                    {
                        #region 山东联网
                        //删除本地待检列表在联网待检列表中不存在的项 
                        for (int i = 0; i < dtWaitCarList.Rows.Count; i++)
                        {
                            string check_jylsh = dtWaitCarList.Rows[i]["JYLSH"].ToString();
                            string check_jycs = dtWaitCarList.Rows[i]["JCCS"].ToString();
                            DataRow[] dr_check_alive_waitlist = dtWaitCarListInNet.Select("jylsh='" + check_jylsh + "' and jycs='" + check_jycs + "'");
                            if (dr_check_alive_waitlist == null || dr_check_alive_waitlist.Count() == 0)
                            {
                                //本机待检车辆信息在网上查不到，则删除本地已有待检车辆信息
                                try
                                {
                                    string strDeleteResult = "状态：流水号" + check_jylsh + "，检验次数" + check_jycs;
                                    if (carinfodb.deleteCarInWaitlist(check_jylsh, check_jycs))//删除本地待检列表
                                        strDeleteResult += "本地待检列表删除成功|";
                                    else
                                        strDeleteResult += "本地待检列表删除失败|";
                                    if (carinfodb.DeleteCarTestStatus(check_jylsh, check_jycs))//删除车辆检测状态表
                                        strDeleteResult += "本地车辆检测状态表删除成功|";
                                    else
                                        strDeleteResult += "本地车辆检测状态表删除失败|";
                                    DataRow[] ReadyToDelete = dt_ReadyUpload.Select("检验流水号 = '" + check_jylsh + "' and 检验次数 = '" + check_jycs + "'");
                                    if (ReadyToDelete.Count() > 0)//软件界面有这个检验流水号及检验次数的记录就删除
                                    {
                                        for (int j = 0; j < ReadyToDelete.Count(); j++)
                                        {
                                            dt_ReadyUpload.Rows.Remove(ReadyToDelete[j]);
                                        }
                                        strDeleteResult += "数据上传软件界面记录删除成功";
                                    }
                                    strDeleteResult += "数据上传软件界面无记录";
                                    tSSL_Status.Text = strDeleteResult;
                                    FileOpreate.SaveLog(strDeleteResult, "待检车辆信息删除", 1);
                                }
                                catch (Exception ex)
                                {
                                    FileOpreate.SaveLog("删除NET上不存在待检车辆信息错误" + ex.Message, "待检列表同步错误", 4);
                                }
                            }
                        }
                        //同步待检列表
                        try
                        {
                            for (int i = 0; i < dtWaitCarListInNet.Rows.Count; i++)
                            {
                                string jcff = "";
                                switch (dtWaitCarListInNet.Rows[i]["jcff"].ToString())
                                {
                                    case "1":
                                        if (FileOpreate.jcff.sds)
                                        {
                                            jcff = "SDS";
                                            break;
                                        }
                                        else
                                            continue;
                                    case "2":
                                        if (FileOpreate.jcff.asm)
                                        {
                                            jcff = "ASM";
                                            break;
                                        }
                                        else
                                            continue;
                                    case "3":
                                        if (FileOpreate.jcff.vmas)
                                        {
                                            jcff = "VMAS";
                                            break;
                                        }
                                        else
                                            continue;
                                    case "4":
                                        if (FileOpreate.jcff.jzjs)
                                        {
                                            jcff = "JZJS";
                                            break;
                                        }
                                        else
                                            continue;
                                    case "5":
                                        if (FileOpreate.jcff.zyjs)
                                        {
                                            jcff = "ZYJS";
                                            break;
                                        }
                                        else
                                            continue;
                                    case "8":
                                        if (FileOpreate.jcff.sds)
                                        {
                                            jcff = "SDSM";
                                            break;
                                        }
                                        else
                                            continue;
                                    default:
                                        FileOpreate.SaveLog("该检测线不支持" + dtWaitCarListInNet.Rows[i]["hphm"].ToString() + "要求使用的检测方法：" + dtWaitCarListInNet.Rows[i]["jcff"].ToString(), "InterfaceError4", 1);
                                        continue;
                                }
                                string jylsh = dtWaitCarListInNet.Rows[i]["jylsh"].ToString();
                                string jycs = dtWaitCarListInNet.Rows[i]["jycs"].ToString();
                                string hphm = dtWaitCarListInNet.Rows[i]["hphm"].ToString();

                                DataRow[] dr_CheckWaitList = dtWaitCarList.Select("JYLSH = '" + jylsh + "' and JCCS = '" + jycs + "'");//查询待检列表中是否存在该车
                                DataRow[] dr_CheckAlreadUpload = dt_AlreadyUpload.Select("检验流水号 = '" + jylsh + "' and 检验次数 = '" + jycs + "'");//上传已成功表是否存在该车

                                if (dr_CheckWaitList.Count() == 0 && dr_CheckAlreadUpload.Count() == 0)//说明：待检车辆没有该车信息，先更新该车信息、再更新待检列表信息
                                {
                                    //1、获取车辆信
                                    string error_info2 = "";
                                    DataTable dt_CarInfoInNet = InterfaceUpload.GetCarInfoByLshSD(jylsh, jycs, out error_info2);
                                    if (dt_CarInfoInNet != null)
                                    {
                                        //2、更新车辆信息
                                        CARINF new_car = new CARINF();
                                        new_car.CLHP = dt_CarInfoInNet.Rows[0]["hphm"].ToString();
                                        new_car.CPYS = dt_CarInfoInNet.Rows[0]["hpys"].ToString();
                                        new_car.HPZL = dt_CarInfoInNet.Rows[0]["hpzl"].ToString();
                                        new_car.CLLX = dt_CarInfoInNet.Rows[0]["cllx"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("cpxh"))
                                            new_car.XH = dt_CarInfoInNet.Rows[0]["cpxh"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("cpmc"))
                                            new_car.PP = dt_CarInfoInNet.Rows[0]["cpmc"].ToString();
                                        new_car.CLSBM = dt_CarInfoInNet.Rows[0]["clsbm"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("clscqy"))
                                            new_car.SCQY = dt_CarInfoInNet.Rows[0]["clscqy"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("fdjxh"))
                                            new_car.FDJXH = dt_CarInfoInNet.Rows[0]["fdjxh"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("fdjscqy"))
                                            new_car.FDJSCQY = dt_CarInfoInNet.Rows[0]["fdjscqy"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("fdjh"))
                                            new_car.FDJHM = dt_CarInfoInNet.Rows[0]["fdjh"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("fdjpl"))
                                            new_car.FDJPL = dt_CarInfoInNet.Rows[0]["fdjpl"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("fdjedzs"))
                                            new_car.EDZS = dt_CarInfoInNet.Rows[0]["fdjedzs"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("fdjedgl"))
                                            new_car.EDGL = dt_CarInfoInNet.Rows[0]["fdjedgl"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("syxz"))
                                            new_car.SYXZ = dt_CarInfoInNet.Rows[0]["syxz"].ToString();
                                        new_car.ZCRQ = DateTime.Parse(dt_CarInfoInNet.Rows[0]["ccdjrq"].ToString());
                                        new_car.SCRQ = DateTime.Parse(dt_CarInfoInNet.Rows[0]["ccrq"].ToString());
                                        if (dt_CarInfoInNet.Columns.Contains("czmc"))
                                            new_car.CZ = dt_CarInfoInNet.Rows[0]["czmc"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("lxdh"))
                                            new_car.LXDH = dt_CarInfoInNet.Rows[0]["lxdh"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("lxdz"))
                                            new_car.CZDZ = dt_CarInfoInNet.Rows[0]["lxdz"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("pfbz"))
                                            new_car.ZXBZ = dt_CarInfoInNet.Rows[0]["pfbz"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("bsqxs"))
                                            new_car.BSQXS = dt_CarInfoInNet.Rows[0]["bsqxs"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("jqfs"))
                                            new_car.JQFS = dt_CarInfoInNet.Rows[0]["jqfs"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("ryzl"))
                                            new_car.RLZL = dt_CarInfoInNet.Rows[0]["ryzl"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("gyfs"))
                                            new_car.GYFS = dt_CarInfoInNet.Rows[0]["gyfs"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("qdfs"))
                                            new_car.QDXS = dt_CarInfoInNet.Rows[0]["qdfs"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("zdzzl"))
                                            new_car.ZZL = dt_CarInfoInNet.Rows[0]["zdzzl"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("jzzl"))
                                            new_car.JZZL = dt_CarInfoInNet.Rows[0]["jzzl"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("zbzl"))
                                            new_car.ZBZL = dt_CarInfoInNet.Rows[0]["zbzl"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("qgs"))
                                            new_car.QGS = dt_CarInfoInNet.Rows[0]["qgs"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("obd"))
                                            new_car.OBD = dt_CarInfoInNet.Rows[0]["obd"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("qdltqy"))
                                            new_car.QDLTQY = dt_CarInfoInNet.Rows[0]["qdltqy"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("chzhq"))
                                            new_car.JHZZ = dt_CarInfoInNet.Rows[0]["chzhq"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("rygg"))
                                            new_car.RYPH = dt_CarInfoInNet.Rows[0]["rygg"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("sjcys"))
                                            new_car.HDZK = dt_CarInfoInNet.Rows[0]["sjcys"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("ssxq"))
                                            new_car.SSXQ = dt_CarInfoInNet.Rows[0]["ssxq"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("dws"))
                                            new_car.DWS = dt_CarInfoInNet.Rows[0]["dws"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("jclx"))
                                            new_car.JCLB = dt_CarInfoInNet.Rows[0]["jclx"].ToString();

                                        if (carinfodb.UpdateOrInsertCarInf(new_car) == true)
                                        {
                                            FileOpreate.SaveLog(hphm + "车辆信息更新成功", "DBWrite1", 1);//3、更新待检列表信息
                                            if (carinfodb.addCarToWaitList(hphm + "T" + DateTime.Now.ToString("yyyyMMddHHmmss"), jylsh, jycs, hphm, dtWaitCarListInNet.Rows[i]["hpys"].ToString(), dtWaitCarListInNet.Rows[i]["hpzl"].ToString(), dtWaitCarListInNet.Rows[i]["ljxslc"].ToString(), jcff, dtWaitCarListInNet.Rows[i]["jczbh"].ToString(), dtWaitCarListInNet.Rows[i]["dlsj"].ToString(), ""))
                                                FileOpreate.SaveLog(hphm + "/" + jcff + "方法待检车辆信息更新成功", "DBWrite2", 1);
                                            else
                                                FileOpreate.SaveLog(hphm + "/" + jcff + "方法待检车辆信息更新失败", "DBError1", 1);
                                        }
                                        else
                                            FileOpreate.SaveLog(hphm + "更新车辆信息表失败，无法加入待检列表", "DBError2", 1);
                                    }
                                    else
                                        FileOpreate.SaveLog(hphm + "/" + jylsh + "/" + jycs + "查询车辆信息失败或未查到该车信息", "InterfaceError5", 1);
                                }
                            }
                        }
                        catch (Exception er)
                        {
                            FileOpreate.SaveLog("错误原因：" + er.Message, "更新车辆信息及待检列表信息错误", 1);
                        }
                        #endregion
                    }
                    else if (FileOpreate.Interface_Area == 1 || FileOpreate.Interface_Area == 2 || FileOpreate.Interface_Area == 3)
                    {
                        #region 辽宁\内蒙\贵州
                        //删除本地待检列表在联网待检列表中不存在的项 
                        for (int i = 0; i < dtWaitCarList.Rows.Count; i++)
                        {
                            string check_jylsh = dtWaitCarList.Rows[i]["JYLSH"].ToString();//本地列表待检车辆流水号
                            string check_jycs = dtWaitCarList.Rows[i]["JCCS"].ToString();//本地列表待检车辆检验次数
                            //查询联网待检车辆是否已存在本地列表中，无时更新车辆信息及本地待检列表
                            DataRow[] dr_check_alive_waitlist = dtWaitCarListInNet.Select("jylsh='" + check_jylsh + "' and testtimes='" + check_jycs + "'");
                            if (dr_check_alive_waitlist == null || dr_check_alive_waitlist.Count() == 0)
                            {
                                //本地待检车辆信息在网上查不到，则删除本地已有待检车辆信息
                                try
                                {
                                    string strDeleteResult = "状态：流水号" + check_jylsh + "，检验次数" + check_jycs;
                                    if (carinfodb.deleteCarInWaitlist(check_jylsh, check_jycs))//删除本地待检列表
                                        strDeleteResult += "本地待检列表删除成功|";
                                    else
                                        strDeleteResult += "本地待检列表删除失败|";
                                    if (carinfodb.DeleteCarTestStatus(check_jylsh, check_jycs))//删除车辆检测状态表
                                        strDeleteResult += "本地车辆检测状态表删除成功|";
                                    else
                                        strDeleteResult += "本地车辆检测状态表删除失败|";
                                    DataRow[] ReadyToDelete = dt_ReadyUpload.Select("检验流水号 = '" + check_jylsh + "' and 检验次数 = '" + check_jycs + "'");
                                    if (ReadyToDelete.Count() > 0)//软件界面有这个检验流水号及检验次数的记录就删除
                                    {
                                        for (int j = 0; j < ReadyToDelete.Count(); j++)
                                        {
                                            dt_ReadyUpload.Rows.Remove(ReadyToDelete[j]);
                                        }
                                        strDeleteResult += "数据上传软件界面记录删除成功";
                                    }
                                    strDeleteResult += "数据上传软件界面无记录";
                                    tSSL_Status.Text = strDeleteResult;
                                    FileOpreate.SaveLog(strDeleteResult, "待检车辆信息删除", 1);
                                }
                                catch (Exception ex)
                                {
                                    FileOpreate.SaveLog("删除NET上不存在待检车辆信息错误" + ex.Message, "待检列表同步错误", 4);
                                }
                            }
                        }
                        //同步待检列表
                        try
                        {
                            for (int i = 0; i < dtWaitCarListInNet.Rows.Count; i++)
                            {
                                string jcff = "";
                                string jylsh = dtWaitCarListInNet.Rows[i]["jylsh"].ToString();
                                string jycs = dtWaitCarListInNet.Rows[i]["testtimes"].ToString();
                                string hphm = dtWaitCarListInNet.Rows[i]["license"].ToString();
                                DataRow[] dr_CheckWaitList = dtWaitCarList.Select("JYLSH = '" + jylsh + "' and JCCS = '" + jycs + "'");//查询待检列表中是否存在该车
                                DataRow[] dr_CheckAlreadUpload = dt_AlreadyUpload.Select("检验流水号 = '" + jylsh + "' and 检验次数 = '" + jycs + "'");//上传已成功表是否存在该车

                                //待检车辆表及上传成功表中均没有该车信息时，先更新该车信息、再更新待检列表信息
                                /*
                                有选择项的传代码、检测软件内部判断
                                */
                                if (dr_CheckWaitList.Count() == 0 && dr_CheckAlreadUpload.Count() == 0)
                                {
                                    //1、获取车辆信
                                    string error_info2 = "";
                                    DataTable dt_CarInfoInNet = InterfaceUpload.GetCarInfoByLshLN(jylsh, jycs, out error_info2);
                                    if (dt_CarInfoInNet != null && dt_CarInfoInNet.Rows.Count > 0)
                                    {
                                        switch (dtWaitCarListInNet.Rows[i]["testtype"].ToString())
                                        {
                                            case "1":
                                                if (FileOpreate.jcff.sds)
                                                {
                                                    if (dt_CarInfoInNet.Rows[0]["vehicletype"].ToString().StartsWith("M"))
                                                        jcff = "SDSM";
                                                    else
                                                        jcff = "SDS";
                                                    break;
                                                }
                                                else
                                                    continue;
                                            case "2":
                                                if (FileOpreate.jcff.asm)
                                                {
                                                    jcff = "ASM";
                                                    break;
                                                }
                                                else
                                                    continue;
                                            case "3":
                                                if (FileOpreate.jcff.vmas)
                                                {
                                                    jcff = "VMAS";
                                                    break;
                                                }
                                                else
                                                    continue;
                                            case "4":
                                                if (FileOpreate.jcff.jzjs)
                                                {
                                                    jcff = "JZJS";
                                                    break;
                                                }
                                                else
                                                    continue;
                                            case "5":
                                                if (FileOpreate.jcff.zyjs)
                                                {
                                                    jcff = "ZYJS";
                                                    break;
                                                }
                                                else
                                                    continue;
                                            default:
                                                FileOpreate.SaveLog("该检测线不支持" + dtWaitCarListInNet.Rows[i]["license"].ToString() + "要求使用的检测方法：" + dtWaitCarListInNet.Rows[i]["jcff"].ToString(), "InterfaceError4", 1);
                                                continue;
                                        }
                                        //2、更新车辆信息
                                        CARINF new_car = new CARINF();
                                        new_car.CLHP = dt_CarInfoInNet.Rows[0]["license"].ToString();
                                        new_car.CPYS = dt_CarInfoInNet.Rows[0]["licensetype"].ToString();
                                        new_car.HPZL = dt_CarInfoInNet.Rows[0]["licensecode"].ToString();
                                        new_car.CLLX = dt_CarInfoInNet.Rows[0]["vehicletype"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("vehiclemodel"))
                                            new_car.XH = dt_CarInfoInNet.Rows[0]["vehiclemodel"].ToString();
                                        else
                                            new_car.XH = "";
                                        if (dt_CarInfoInNet.Columns.Contains("cpmc"))
                                            new_car.PP = dt_CarInfoInNet.Rows[0]["cpmc"].ToString();
                                        else
                                            new_car.PP = "";
                                        new_car.CLSBM = dt_CarInfoInNet.Rows[0]["vin"].ToString();
                                        if (dt_CarInfoInNet.Columns.Contains("clscqy"))
                                            new_car.SCQY = dt_CarInfoInNet.Rows[0]["clscqy"].ToString();
                                        else
                                            new_car.SCQY = "";
                                        if (dt_CarInfoInNet.Columns.Contains("engine"))
                                            new_car.FDJXH = dt_CarInfoInNet.Rows[0]["engine"].ToString();
                                        else
                                            new_car.FDJXH = "";
                                        if (dt_CarInfoInNet.Columns.Contains("enginemanuf"))
                                            new_car.FDJSCQY = dt_CarInfoInNet.Rows[0]["enginemanuf"].ToString();
                                        else
                                            new_car.FDJSCQY = "";
                                        if (dt_CarInfoInNet.Columns.Contains("fdjh"))
                                            new_car.FDJHM = dt_CarInfoInNet.Rows[0]["fdjh"].ToString();
                                        else
                                            new_car.FDJHM = "";
                                        if (dt_CarInfoInNet.Columns.Contains("ed"))
                                            new_car.FDJPL = dt_CarInfoInNet.Rows[0]["ed"].ToString();
                                        else
                                            new_car.FDJPL = "";
                                        if (dt_CarInfoInNet.Columns.Contains("enginespeed"))
                                            new_car.EDZS = dt_CarInfoInNet.Rows[0]["enginespeed"].ToString();
                                        else
                                            new_car.EDZS = "";
                                        if (dt_CarInfoInNet.Columns.Contains("enginepower"))
                                            new_car.EDGL = dt_CarInfoInNet.Rows[0]["enginepower"].ToString();
                                        else
                                            new_car.EDGL = "";
                                        if (dt_CarInfoInNet.Columns.Contains("usetype"))
                                            new_car.SYXZ = dt_CarInfoInNet.Rows[0]["usetype"].ToString();
                                        else
                                            new_car.SYXZ = "";
                                        new_car.ZCRQ = DateTime.Parse(dt_CarInfoInNet.Rows[0]["registerdate"].ToString());
                                        new_car.SCRQ = DateTime.Parse(dt_CarInfoInNet.Rows[0]["mdate"].ToString());
                                        if (dt_CarInfoInNet.Columns.Contains("owner"))
                                            new_car.CZ = dt_CarInfoInNet.Rows[0]["owner"].ToString();
                                        else
                                            new_car.CZ = "";
                                        if (dt_CarInfoInNet.Columns.Contains("lxdh"))
                                            new_car.LXDH = dt_CarInfoInNet.Rows[0]["lxdh"].ToString();
                                        else
                                            new_car.LXDH = "";
                                        if (dt_CarInfoInNet.Columns.Contains("lxdz"))
                                            new_car.CZDZ = dt_CarInfoInNet.Rows[0]["lxdz"].ToString();
                                        else
                                            new_car.CZDZ = "";
                                        if (dt_CarInfoInNet.Columns.Contains("standard"))
                                            new_car.ZXBZ = dt_CarInfoInNet.Rows[0]["standard"].ToString();
                                        else
                                            new_car.ZXBZ = "";
                                        if (dt_CarInfoInNet.Columns.Contains("gear"))
                                            new_car.BSQXS = dt_CarInfoInNet.Rows[0]["gear"].ToString();
                                        else
                                            new_car.BSQXS = "";
                                        if (dt_CarInfoInNet.Columns.Contains("airin"))
                                            new_car.JQFS = dt_CarInfoInNet.Rows[0]["airin"].ToString();
                                        else
                                            new_car.JQFS = "";
                                        if (dt_CarInfoInNet.Columns.Contains("fueltype"))
                                            new_car.RLZL = dt_CarInfoInNet.Rows[0]["fueltype"].ToString();
                                        else
                                            new_car.RLZL = "";
                                        if (dt_CarInfoInNet.Columns.Contains("fuelsupply"))
                                            new_car.GYFS = dt_CarInfoInNet.Rows[0]["fuelsupply"].ToString();
                                        else
                                            new_car.GYFS = "";
                                        if (dt_CarInfoInNet.Columns.Contains("drivemode"))
                                            new_car.QDXS = dt_CarInfoInNet.Rows[0]["drivemode"].ToString();
                                        else
                                            new_car.QDXS = "";
                                        if (dt_CarInfoInNet.Columns.Contains("gvm"))
                                            new_car.ZZL = dt_CarInfoInNet.Rows[0]["gvm"].ToString();
                                        else
                                            new_car.ZZL = "";
                                        if (dt_CarInfoInNet.Columns.Contains("rm"))
                                            new_car.JZZL = dt_CarInfoInNet.Rows[0]["rm"].ToString();
                                        else
                                            new_car.JZZL = "";
                                        if (dt_CarInfoInNet.Columns.Contains("zbzl"))
                                            new_car.ZBZL = dt_CarInfoInNet.Rows[0]["zbzl"].ToString();
                                        else
                                            new_car.ZBZL = "";
                                        if (dt_CarInfoInNet.Columns.Contains("cylinders"))
                                            new_car.QGS = dt_CarInfoInNet.Rows[0]["cylinders"].ToString();
                                        else
                                            new_car.QGS = "";
                                        if (dt_CarInfoInNet.Columns.Contains("obd"))
                                            new_car.OBD = dt_CarInfoInNet.Rows[0]["obd"].ToString();
                                        else
                                            new_car.OBD = "";
                                        if (dt_CarInfoInNet.Columns.Contains("fdjccs"))
                                            new_car.CCS = dt_CarInfoInNet.Rows[0]["fdjccs"].ToString();
                                        else
                                            new_car.CCS = "";
                                        if (dt_CarInfoInNet.Columns.Contains("gbwdzz"))
                                            new_car.CHZZ = dt_CarInfoInNet.Rows[0]["gbwdzz"].ToString();
                                        else
                                            new_car.CHZZ = "";
                                        if (dt_CarInfoInNet.Columns.Contains("qdltqy"))
                                            new_car.QDLTQY = dt_CarInfoInNet.Rows[0]["qdltqy"].ToString();
                                        else
                                            new_car.QDLTQY = "";
                                        if (dt_CarInfoInNet.Columns.Contains("chzhq"))
                                            new_car.JHZZ = dt_CarInfoInNet.Rows[0]["chzhq"].ToString();
                                        else
                                            new_car.JHZZ = "";
                                        if (dt_CarInfoInNet.Columns.Contains("rygg"))
                                            new_car.RYPH = dt_CarInfoInNet.Rows[0]["rygg"].ToString();
                                        else
                                            new_car.RYPH = "";
                                        if (dt_CarInfoInNet.Columns.Contains("sjcys"))
                                            new_car.HDZK = dt_CarInfoInNet.Rows[0]["sjcys"].ToString();
                                        else
                                            new_car.HDZK = "";
                                        if (dt_CarInfoInNet.Columns.Contains("ssxq"))
                                            new_car.SSXQ = dt_CarInfoInNet.Rows[0]["ssxq"].ToString();
                                        else
                                            new_car.SSXQ = "";
                                        if (dt_CarInfoInNet.Columns.Contains("dws"))
                                            new_car.DWS = dt_CarInfoInNet.Rows[0]["dws"].ToString();
                                        else
                                            new_car.DWS = "";
                                        if (dt_CarInfoInNet.Columns.Contains("jclx"))
                                            new_car.JCLB = dt_CarInfoInNet.Rows[0]["jclx"].ToString();
                                        else
                                            new_car.JCLB = "";
                                        if (dt_CarInfoInNet.Columns.Contains("fdjzdjgl"))
                                            new_car.ZDJGL = dt_CarInfoInNet.Rows[0]["fdjzdjgl"].ToString();
                                        else
                                            new_car.ZDJGL = "";

                                        if (carinfodb.UpdateOrInsertCarInf(new_car))
                                        {
                                            FileOpreate.SaveLog(hphm + "车辆信息更新成功", "车辆信息更新", 1);
                                            //3、更新待检列表信息
                                            if (carinfodb.addCarToWaitList(hphm + "T" + DateTime.Now.ToString("yyyyMMddHHmmss"), jylsh, jycs, dtWaitCarListInNet.Rows[i]["license"].ToString(), dt_CarInfoInNet.Rows[0]["licensetype"].ToString(), dtWaitCarListInNet.Rows[i]["licensecode"].ToString(), dt_CarInfoInNet.Rows[0]["odometer"].ToString(), jcff, dtWaitCarListInNet.Rows[i]["tsno"].ToString(), dtWaitCarListInNet.Rows[i]["dlsj"].ToString(), dt_CarInfoInNet.Rows[0]["jclry"].ToString()))
                                                FileOpreate.SaveLog(hphm + "|" + jylsh + "|" + jycs + "|" + jcff + "方法待检车辆信息更新成功", "DBWrite2", 1);
                                            else
                                                FileOpreate.SaveLog(hphm + "|" + jylsh + "|" + jycs + "|" + jcff + "方法待检车辆信息更新失败", "DBError1", 1);
                                        }
                                        else
                                        {
                                            tSSL_Status.Text = hphm + "车辆信息更新失败！";
                                            FileOpreate.SaveLog(hphm + "|" + jylsh + "|" + jycs + "车辆信息更新失败", "车辆信息更新", 1);
                                        }
                                    }
                                    else
                                        FileOpreate.SaveLog(hphm + "|" + jylsh + "|" + jycs + "查询车辆信息失败，原因：" + error_info2, "联网查询车辆信息失败", 1);
                                }
                            }
                        }
                        catch (Exception er)
                        {
                            FileOpreate.SaveLog("错误原因：" + er.Message, "更新车辆信息及待检列表信息错误", 1);
                        }
                        #endregion
                    }
                }
                else
                {
                    tSSL_Status.Text = "联网平台无待检车辆！";
                    FileOpreate.SaveLog("联网平台无待检车辆", "待检车辆信息获取", 1);
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog(er.Message, "systemError1", 1);
            }
            #endregion
        }

        //数据上传定时器
        private void timerUploadTestData_Tick(object sender, EventArgs e)
        {
            if (!_whether_Connected)//若同步时间失败则不上传
                return;
            
            try
            {
                DataTable CarTestStatusTable = carinfodb.getCarTestStatus();//获取车辆检测状态表
                #region 显示 
                dt_ReadyUpload.Clear();
                if (CarTestStatusTable != null && CarTestStatusTable.Rows.Count > 0)
                {
                    for (int i = 0; i < CarTestStatusTable.Rows.Count; i++)
                    {
                        DataRow dr = dt_ReadyUpload.NewRow();
                        dr["检验流水号"] = CarTestStatusTable.Rows[i]["JYLSH"].ToString();
                        dr["车辆号牌"] = CarTestStatusTable.Rows[i]["CLHP"].ToString();
                        dr["号牌种类"] = CarTestStatusTable.Rows[i]["HPZL"].ToString();
                        dr["线号"] = CarTestStatusTable.Rows[i]["LINEID"].ToString();
                        dr["检验次数"] = CarTestStatusTable.Rows[i]["JYCS"].ToString();
                        dr["检验时间"] = CarTestStatusTable.Rows[i]["JCSJ"].ToString();
                        dr["检测方法"] = CarTestStatusTable.Rows[i]["JCFF"].ToString();
                        dr["当前状态"] = JcZt[CarTestStatusTable.Rows[i]["ZT"].ToString()];
                        dr["已处理状态"] = CarTestStatusTable.Rows[i]["YCLZT"].ToString() == "6" ? "上传过程/结果数据" : JcZt[CarTestStatusTable.Rows[i]["YCLZT"].ToString()];
                        dr["备注"] = CarTestStatusTable.Rows[i]["BZ"].ToString();
                        dt_ReadyUpload.Rows.Add(dr);
                    }
                }
                dataGridView_Ready_And_Failed.DataSource = dt_ReadyUpload;
                #endregion
                if (CarTestStatusTable != null && CarTestStatusTable.Rows.Count > 0)
                {
                    //逐行判断车辆检测状态是否变化
                    for (int i = 0; i < CarTestStatusTable.Rows.Count; i++)
                    {
                        //提取数据准备启动数据上传线程
                        DataTable dt_jczt = CarTestStatusTable.Clone();
                        dt_jczt.ImportRow(CarTestStatusTable.Rows[i]);

                        //处理提取数据
                        dt_jczt.Rows[0]["HPZL"] = InterfaceUpload.get_hpzl_code[dt_jczt.Rows[0]["HPZL"].ToString()];
                        string tempZT = dt_jczt.Rows[0]["ZT"].ToString();
                        string tempYCLZT = dt_jczt.Rows[0]["YCLZT"].ToString();
                        string jylsh = dt_jczt.Rows[0]["JYLSH"].ToString();
                        string jycs = dt_jczt.Rows[0]["JYCS"].ToString();


                        if (tempZT == "3" || tempZT == "5" || ((tempZT != "1") && (tempZT == tempYCLZT)))//3为联网验证未通过，5为检测未完成退出，tempZT、tempYCLZT相等时不执行上传操作
                            continue;
                        try
                        {
                            switch (dt_jczt.Rows[0]["JCFF"].ToString())
                            {
                                case "ASM":
                                    if (tempZT == "1" || tempZT == "21" || tempZT == "22" || tempZT == "23" || tempZT == "24" || tempZT == "6")
                                    {
                                        if (AsmUploadThread != null && AsmUploadThread.IsAlive)
                                            continue;
                                        AsmUploadThread = new Thread(new ParameterizedThreadStart(UploadASMData));//新建一个过程的上传线程
                                        AsmUploadThread.Start(dt_jczt);//开始上传线程
                                        carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);//临时更新已处理状态到当前状态
                                        ShowStatus(jylsh, jycs, "备注", "ASM数据自动上传中...");
                                        FileOpreate.SaveLog("jylsh:" + jylsh + "|ASM数据自动上传线程已开始", "检测数据上传", 1);
                                    }
                                    break;
                                case "VMAS":
                                    if (tempZT == "1" || tempZT == "31" || tempZT == "32" || tempZT == "33" || tempZT == "6")
                                    {
                                        if (VmasUploadThread != null && VmasUploadThread.IsAlive)
                                            continue;
                                        VmasUploadThread = new Thread(new ParameterizedThreadStart(UploadVMASData));
                                        VmasUploadThread.Start(dt_jczt);
                                        carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);
                                        ShowStatus(jylsh, jycs, "备注", "VMAS数据自动上传中...");
                                        FileOpreate.SaveLog("VMAS数据自动上传线程已开始", "检测数据上传", 1);
                                    }
                                    break;
                                case "SDS":
                                    if (tempZT == "1" || tempZT == "11" || tempZT == "12" || tempZT == "6")
                                    {
                                        if (SdsUploadThread != null && SdsUploadThread.IsAlive)
                                            continue;
                                        SdsUploadThread = new Thread(new ParameterizedThreadStart(UploadSDSData));
                                        SdsUploadThread.Start(dt_jczt);
                                        carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);
                                        ShowStatus(jylsh, jycs, "备注", "SDS数据自动上传中...");
                                        FileOpreate.SaveLog("jylsh:"+ jylsh+"|SDS数据自动上传线程已开始", "检测数据上传", 1);
                                    }
                                    break;
                                case "SDSM":
                                    if (tempZT == "1" || tempZT == "81" || tempZT == "82" || tempZT == "6")
                                    {
                                        if (SdsMUploadThread != null && SdsMUploadThread.IsAlive)
                                            continue;
                                        SdsMUploadThread = new Thread(new ParameterizedThreadStart(UploadSDSMData));
                                        SdsMUploadThread.Start(dt_jczt);
                                        carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);
                                        ShowStatus(jylsh, jycs, "备注", "SDSM数据自动上传中...");
                                        FileOpreate.SaveLog("SDSM数据自动上传线程已开始", "检测数据上传", 1);
                                    }
                                    break;
                                case "JZJS":
                                    if (tempZT == "1" || tempZT == "41" || tempZT == "42" || tempZT == "43" || tempZT == "6")
                                    {
                                        if (JzjsUploadThread != null && JzjsUploadThread.IsAlive)
                                            continue;
                                        JzjsUploadThread = new Thread(new ParameterizedThreadStart(UploadJZJSData));
                                        JzjsUploadThread.Start(dt_jczt);
                                        carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);
                                        ShowStatus(jylsh, jycs, "备注", "JZJS数据自动上传中...");
                                        FileOpreate.SaveLog("JZJS数据自动上传线程已开始", "检测数据上传", 1);
                                    }
                                    break;
                                case "ZYJS":
                                    if (tempZT == "1" || tempZT == "51" || tempZT == "52" || tempZT == "53" || tempZT == "6")
                                    {
                                        if (ZyjsUploadThread != null && ZyjsUploadThread.IsAlive)
                                            continue;
                                        ZyjsUploadThread = new Thread(new ParameterizedThreadStart(UploadZYJSData));
                                        ZyjsUploadThread.Start(dt_jczt);
                                        carinfodb.UpdateCarTestStatusYCLZT(tempZT, jylsh, jycs);
                                        ShowStatus(jylsh, jycs, "备注", "ZYJS数据自动上传中...");
                                        FileOpreate.SaveLog("ZYJS数据自动上传线程已开始", "检测数据上传", 1);
                                    }
                                    break;
                            }
                        }
                        catch (Exception er)
                        {
                            FileOpreate.SaveLog("开启线程失败，原因：" + er.Message, "检测数据上传", 1);
                        }
                    }
                    CarTestStatusTable.Clear();
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("失败原因：" + er.Message, "自动上传错误", 1);
            }
        }
        Thread AsmUploadThread = null;
        Thread VmasUploadThread = null;
        Thread SdsUploadThread = null;
        Thread SdsMUploadThread = null;
        Thread JzjsUploadThread = null;
        Thread ZyjsUploadThread = null;
        
        #endregion

        #region 数据上报线程，当ZT与YCLZT不等时执行一次上传
        private void UploadASMData(object obj)
        {
            try
            {
                DataTable dt_test_status = ((DataTable)obj).Copy();

                if (dt_test_status.Rows.Count <= 0)
                {
                    //carinfodb.UpdateCarTestStatusYCLZT(dr_temp["YCLZT"].ToString(), dr_temp["JYLSH"].ToString(), dr_temp["JYCS"].ToString());
                    FileOpreate.SaveLog("ASM上传线程传入数据为空" + obj.ToString(), "ASM上传线程执行失败", 1);
                    return;
                }
                string jylsh = dt_test_status.Rows[0]["JYLSH"].ToString();
                string jycs = dt_test_status.Rows[0]["JYCS"].ToString();
                string zt = dt_test_status.Rows[0]["ZT"].ToString();
                string yclzt = dt_test_status.Rows[0]["YCLZT"].ToString();

                try
                {
                    FileOpreate.SaveLog("流水号：" + jylsh + "数据上传准备", "ASM上传过程", 1);
                    if (zt == "1")
                    {
                        //发请求开始——KS001，解析开始是否同意，更新检测状态，并更新YCLZT到1
                        string error_info = "";
                        if (UpdateProjectStart(dt_test_status.Rows[0], out error_info))
                        {
                            if (carinfodb.UpdateCarTestStatusZT("2", jylsh, jycs, error_info))
                            {
                                //yclzt = "2";
                                carinfodb.UpdateCarTestStatusYCLZT("2", jylsh, jycs);//将该条记录置为上传失败记录
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目开始发送成功", "ASM上传过程", 1);
                            }
                            else
                                FileOpreate.SaveLog("流水号：" + jylsh + "更新允许开始状态失败", "ASM上传过程", 1);
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusZT("3", jylsh, jycs, error_info);
                            carinfodb.UpdateCarTestStatusYCLZT("0", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "不允许开始，原因：" + error_info, "ASM上传过程", 1);
                            return;
                        }
                    }
                    if ((zt == "21" || zt == "22" || zt == "23" || zt == "24" || zt == "6") && yclzt == "2")
                    {
                        //发21照片，更新YCLZT到21
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "21"))
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("21", jylsh, jycs);
                            //yclzt = "21";
                            FileOpreate.SaveLog("流水号：" + jylsh + "21发送成功", "ASM上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "21发送失败", "ASM上传过程", 1);
                    }
                    if ((zt == "22" || zt == "23" || zt == "24" || zt == "6") && yclzt == "21")
                    {
                        //发22，更新YCLZT到22
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "22"))
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("22", jylsh, jycs);
                            //yclzt = "22";
                            FileOpreate.SaveLog("流水号：" + jylsh + "22发送成功", "ASM上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "22发送失败", "ASM上传过程", 1);
                    }
                    if ((zt == "23" || zt == "24" || zt == "6") && yclzt == "22")
                    {
                        //发23，更新YCLZT到23
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "23"))
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("23", jylsh, jycs);
                            //yclzt = "23";
                            FileOpreate.SaveLog("流水号：" + jylsh + "23发送成功", "ASM上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "23发送失败", "ASM上传过程", 1);
                    }
                    if ((zt == "24" || zt == "6") && yclzt == "23")
                    {
                        //发24，更新YCLZT到24
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "24"))
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("24", jylsh, jycs);
                            //yclzt = "24";
                            FileOpreate.SaveLog("流水号：" + jylsh + "24发送成功", "ASM上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "24发送失败", "ASM上传过程", 1);
                    }
                    if (zt == "6" && yclzt == "24")
                    {
                        string test_result = "";
                        if (UpdateResult("ASM", jylsh, jycs, out test_result))
                        {
                            FileOpreate.SaveLog("流水号：" + jylsh + "结果数据发送成功", "ASM上传过程", 1);
                            if (UpdateProjectStop(dt_test_status.Rows[0], test_result))
                            {
                                UpdateStatus(jylsh, jycs, "已处理状态", "上传完毕");
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目结束指令发送成功，所有数据上传完毕", "ASM上传过程", 1);
                                //将该条数据添加到已上传数据中，删除车辆检测状态表中该条数据，记录上传成功日志
                                DataRow dr = dt_AlreadyUpload.NewRow();
                                dr["检验流水号"] = dt_test_status.Rows[0]["JYLSH"].ToString();
                                dr["车辆号牌"] = dt_test_status.Rows[0]["CLHP"].ToString();
                                dr["号牌种类"] = dt_test_status.Rows[0]["HPZL"].ToString();
                                dr["线号"] = dt_test_status.Rows[0]["LINEID"].ToString();
                                dr["检验次数"] = dt_test_status.Rows[0]["JYCS"].ToString();
                                dr["检验时间"] = dt_test_status.Rows[0]["JCSJ"].ToString();
                                dr["检测方法"] = dt_test_status.Rows[0]["JCFF"].ToString();
                                dr["上传成功时间"] = System.DateTime.Now.ToString();
                                dt_AlreadyUpload.Rows.Add(dr);
                                carinfodb.DeleteCarTestStatus(jylsh, jycs);
                                carinfodb.deleteCarInWaitlist(jylsh, jycs);
                            }
                            else
                            {
                                carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs,test_result);
                                FileOpreate.SaveLog("流水号：" + jylsh + "检测结束上传失败", "ASM上传过程", 1);
                            }
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, test_result);
                            FileOpreate.SaveLog("流水号：" + jylsh + "上传失败", "ASM上传过程", 1);
                        }

                    }
                    
                    UpdateStatus(jylsh, jycs, "备注", "当前数据上传完");
                }
                catch (Exception er)
                {
                    //更新YCLZT状态到实际执行到的位置
                    carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, er.Message);
                    FileOpreate.SaveLog("数据上传错误原因：" + er.Message, "ASM上传错误", 1);
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("线程运行错误原因：" + er.Message, "ASM上传线程错误", 1);
            }
        }
        private void UploadVMASData(object obj)
        {
            try
            {
                DataTable dt_test_status = ((DataTable)obj).Copy();

                if (dt_test_status.Rows.Count <= 0)
                {
                    //carinfodb.UpdateCarTestStatusYCLZT(dr_temp["YCLZT"].ToString(), dr_temp["JYLSH"].ToString(), dr_temp["JYCS"].ToString());
                    FileOpreate.SaveLog("VMAS上传线程传入数据为空" + obj.ToString(), "VAMS上传线程执行失败", 1);
                    return;
                }
                string jylsh = dt_test_status.Rows[0]["JYLSH"].ToString();
                string jycs = dt_test_status.Rows[0]["JYCS"].ToString();
                string zt = dt_test_status.Rows[0]["ZT"].ToString();
                string yclzt = dt_test_status.Rows[0]["YCLZT"].ToString();

                try
                {
                    FileOpreate.SaveLog("流水号：" + jylsh + "VMAS数据上传准备", "VMAS上传过程", 1);
                    if (zt == "1")
                    {
                        //发请求开始——KS001，解析开始是否同意，更新检测状态，并更新YCLZT到1
                        string error_info = "";
                        if (UpdateProjectStart(dt_test_status.Rows[0], out error_info))
                        {
                            if (carinfodb.UpdateCarTestStatusZT("2", jylsh, jycs, error_info))
                            {
                                carinfodb.UpdateCarTestStatusYCLZT("2", jylsh, jycs);
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目开始发送成功", "VMAS上传过程", 1);
                            }
                            else
                                FileOpreate.SaveLog("流水号：" + jylsh + "更新允许开始状态失败", "VMAS上传过程", 1);
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusZT("3", jylsh, jycs, error_info);
                            carinfodb.UpdateCarTestStatusYCLZT("0", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "不允许开始，原因：" + error_info, "VMAS上传过程", 1);
                            return;
                        }
                    }
                    if ((zt == "31" || zt == "32" || zt == "33" || zt == "6") && yclzt == "2")
                    {
                        //发31照片，更新YCLZT到31
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "31"))
                        {
                           // yclzt = "31";
                            carinfodb.UpdateCarTestStatusYCLZT("31", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "31发送成功", "VMAS上传过程", 1);
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs);//将该条记录置为上传失败记录
                            FileOpreate.SaveLog("流水号：" + jylsh + "31发送失败", "VMAS上传过程", 1);
                        }
                    }
                    if ((zt == "32" || zt == "33" || zt == "6") && yclzt == "31")
                    {
                        //发32
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "32"))
                        {
                            //yclzt = "32";
                            carinfodb.UpdateCarTestStatusYCLZT("32", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "32发送成功", "VMAS上传过程", 1);
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs);//将该条记录置为上传失败记录
                            FileOpreate.SaveLog("流水号：" + jylsh + "32发送失败", "VMAS上传过程", 1);
                        }
                        //更新YCLZT到32
                    }
                    if ((zt == "33" || zt == "6") && yclzt == "32")
                    {
                        //发33
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "33"))
                        {
                            //yclzt = "33";
                            carinfodb.UpdateCarTestStatusYCLZT("33", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "33发送成功", "VMAS上传过程", 1);
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs);//将该条记录置为上传失败记录
                            FileOpreate.SaveLog("流水号：" + jylsh + "33发送失败", "VMAS上传过程", 1);
                        }
                        //更新YCLZT到33
                    }
                    if (zt == "6" && yclzt == "33")
                    {
                        string test_result = "";
                        if (UpdateResult("VMAS",jylsh, jycs, out test_result))
                        {
                            FileOpreate.SaveLog("流水号：" + jylsh + "结果数据发送成功", "VMAS上传过程", 1);
                            if (UpdateProjectStop(dt_test_status.Rows[0], test_result))
                            {
                                UpdateStatus(jylsh, jycs, "已处理状态", "上传完毕");
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目结束发送成功，所有数据上传完毕", "VMAS上传过程", 1);
                                //更新其他状态
                                DataRow dr = dt_AlreadyUpload.NewRow();
                                dr["检验流水号"] = dt_test_status.Rows[0]["JYLSH"].ToString();
                                dr["车辆号牌"] = dt_test_status.Rows[0]["CLHP"].ToString();
                                dr["号牌种类"] = dt_test_status.Rows[0]["HPZL"].ToString();
                                dr["线号"] = dt_test_status.Rows[0]["LINEID"].ToString();
                                dr["检验次数"] = dt_test_status.Rows[0]["JYCS"].ToString();
                                dr["检验时间"] = dt_test_status.Rows[0]["JCSJ"].ToString();
                                dr["检测方法"] = dt_test_status.Rows[0]["JCFF"].ToString();
                                dr["上传成功时间"] = System.DateTime.Now.ToString();
                                dt_AlreadyUpload.Rows.Add(dr);
                                carinfodb.DeleteCarTestStatus(jylsh, jycs);
                                carinfodb.deleteCarInWaitlist(jylsh, jycs);
                            }
                            else
                            {
                                carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, test_result);//将该条记录置为上传失败记录
                                FileOpreate.SaveLog("流水号：" + jylsh + "检测结束上传失败", "VMAS上传过程", 1);
                            }
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs,test_result);//将该条记录置为上传失败记录
                            FileOpreate.SaveLog("流水号：" + jylsh + "上传失败", "VMAS上传过程", 1);
                        }
                    }

                    //更新YCLZT状态到实际执行到的位置
                    UpdateStatus(jylsh, jycs, "备注", "当前数据上传完");
                }
                catch (Exception er)
                {
                    //更新YCLZT状态到实际执行到的位置
                    carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs,er.Message);
                    FileOpreate.SaveLog("数据上传错误原因：" + er.Message, "Vmas上传错误", 1);
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("线程运行错误原因：" + er.Message, "Vmas上传线程错误", 1);
            }
        }
        private void UploadSDSData(object obj)
        {
            try
            {
                DataTable dt_test_status = ((DataTable)obj).Copy();

                if (dt_test_status.Rows.Count <= 0)
                {
                    //carinfodb.UpdateCarTestStatusYCLZT(dr_temp["YCLZT"].ToString(), dr_temp["JYLSH"].ToString(), dr_temp["JYCS"].ToString());
                    FileOpreate.SaveLog("SDS上传线程传入数据为空" + obj.ToString(), "SDS上传线程执行失败", 1);
                    return;
                }
                string jylsh = dt_test_status.Rows[0]["JYLSH"].ToString();
                string jycs = dt_test_status.Rows[0]["JYCS"].ToString();
                string zt = dt_test_status.Rows[0]["ZT"].ToString();
                string yclzt = dt_test_status.Rows[0]["YCLZT"].ToString();

                try
                {
                    FileOpreate.SaveLog("流水号：" + jylsh + "SDS数据上传准备", "SDS上传过程", 1);
                    if (zt == "1")
                    {
                        //发请求开始——KS001，解析开始是否同意，更新检测状态，并更新YCLZT到1
                        string error_info = "";
                        if (UpdateProjectStart(dt_test_status.Rows[0], out error_info))
                        {
                            if (carinfodb.UpdateCarTestStatusZT("2", jylsh, jycs, error_info))
                            {
                                yclzt = "2";
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目开始发送成功", "SDS上传过程", 1);
                            }
                            else
                                FileOpreate.SaveLog("流水号：" + jylsh + "更新允许开始状态失败", "SDS上传过程", 1);
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusZT("3", jylsh, jycs, error_info);
                            carinfodb.UpdateCarTestStatusYCLZT("0", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "不允许开始，原因：" + error_info, "SDS上传过程", 1);
                            return;
                        }
                    }
                    if ((zt == "11" || zt == "12" || zt == "6") && yclzt == "2")
                    {
                        //发11照片
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "11"))
                        {
                            //yclzt = "11";
                            carinfodb.UpdateCarTestStatusYCLZT("11", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "11发送成功", "SDS上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "11发送失败", "SDS上传过程", 1);
                        //更新YCLZT到11
                    }
                    if ((zt == "12" || zt == "6") && yclzt == "11")
                    {
                        //发12
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "12"))
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("12", jylsh, jycs);
                            //yclzt = "12";
                            FileOpreate.SaveLog("流水号：" + jylsh + "12发送成功", "SDS上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "12发送失败", "SDS上传过程", 1);
                        //更新YCLZT到12
                    }
                    if (zt == "6" && yclzt == "12")
                    {
                        string test_result = "";
                        if (UpdateResult("SDS", jylsh, jycs, out test_result))
                        {
                            FileOpreate.SaveLog("流水号：" + jylsh + "结果数据发送成功", "SDS上传过程", 1);
                            if (UpdateProjectStop(dt_test_status.Rows[0], test_result))
                            {
                                UpdateStatus(jylsh, jycs, "已处理状态", "上传完毕");
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目结束命令发送成功", "SDS上传过程", 1);
                                //更新其他状态
                                DataRow dr = dt_AlreadyUpload.NewRow();
                                dr["检验流水号"] = dt_test_status.Rows[0]["JYLSH"].ToString();
                                dr["车辆号牌"] = dt_test_status.Rows[0]["CLHP"].ToString();
                                dr["号牌种类"] = dt_test_status.Rows[0]["HPZL"].ToString();
                                dr["线号"] = dt_test_status.Rows[0]["LINEID"].ToString();
                                dr["检验次数"] = dt_test_status.Rows[0]["JYCS"].ToString();
                                dr["检验时间"] = dt_test_status.Rows[0]["JCSJ"].ToString();
                                dr["检测方法"] = dt_test_status.Rows[0]["JCFF"].ToString();
                                dr["上传成功时间"] = System.DateTime.Now.ToString();
                                dt_AlreadyUpload.Rows.Add(dr);
                                FileOpreate.SaveLog("流水号：" + jylsh + "所有数据上传完毕", "SDS上传过程", 1);
                                carinfodb.DeleteCarTestStatus(jylsh, jycs);
                                carinfodb.deleteCarInWaitlist(jylsh, jycs);
                            }
                            else
                            {
                                carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, test_result);//将该条记录置为上传失败记录
                                FileOpreate.SaveLog("流水号：" + jylsh + "检测结束上传失败", "SDS上传过程", 1);
                            }

                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, test_result);//将该条记录置为上传失败记录
                            FileOpreate.SaveLog("流水号：" + jylsh + "上传失败", "SDS上传过程", 1);
                        }
                        //更新YCLZT到6
                    }

                    //更新YCLZT状态到实际执行到的位置
                    //carinfodb.UpdateCarTestStatusYCLZT(yclzt, jylsh, jycs);
                    UpdateStatus(jylsh, jycs, "备注", "当前数据上传完");
                }
                catch (Exception er)
                {
                    //更新YCLZT状态到实际执行到的位置
                    carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, er.Message);
                    FileOpreate.SaveLog("数据上传错误原因：" + er.Message, "SDS上传错误", 1);
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("线程运行错误原因：" + er.Message, "SDS上传线程错误", 1);
            }
        }
        private void UploadSDSMData(object obj)
        {
            try
            {
                DataTable dt_test_status = ((DataTable)obj).Copy();

                if (dt_test_status.Rows.Count <= 0)
                {
                    //carinfodb.UpdateCarTestStatusYCLZT(dr_temp["YCLZT"].ToString(), dr_temp["JYLSH"].ToString(), dr_temp["JYCS"].ToString());
                    FileOpreate.SaveLog("SDSM上传线程传入数据为空" + obj.ToString(), "SDSM上传线程执行失败", 1);
                    return;
                }
                string jylsh = dt_test_status.Rows[0]["JYLSH"].ToString();
                string jycs = dt_test_status.Rows[0]["JYCS"].ToString();
                string zt = dt_test_status.Rows[0]["ZT"].ToString();
                string yclzt = dt_test_status.Rows[0]["YCLZT"].ToString();
                try
                {
                    FileOpreate.SaveLog("流水号：" + jylsh + "SDSM数据上传准备", "SDSM上传过程", 1);
                    if (zt == "1")
                    {
                        //发请求开始——KS001，解析开始是否同意，更新检测状态，并更新YCLZT到1
                        string error_info = "";
                        if (UpdateProjectStart(dt_test_status.Rows[0], out error_info))
                        {
                            if (carinfodb.UpdateCarTestStatusZT("2", jylsh, jycs, error_info))
                            {
                                yclzt = "2";
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目开始发送成功", "SDS上传过程", 1);
                            }
                            else
                                FileOpreate.SaveLog("流水号：" + jylsh + "更新允许开始状态失败", "SDS上传过程", 1);
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusZT("3", jylsh, jycs, error_info);
                            carinfodb.UpdateCarTestStatusYCLZT("0", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "不允许开始，原因：" + error_info, "SDS上传过程", 1);
                            return;
                        }
                    }
                    if ((zt == "81" || zt == "82" || zt == "6") && yclzt == "2")
                    {
                        //发11照片
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "81"))
                        {
                            //yclzt = "81";
                            carinfodb.UpdateCarTestStatusYCLZT("81", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "81发送成功", "SDSM上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "81发送失败", "SDSM上传过程", 1);
                        //更新YCLZT到81
                    }
                    if ((zt == "82" || zt == "6") && yclzt == "81")
                    {
                        //发82
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "82"))
                        {
                            //yclzt = "82";
                            carinfodb.UpdateCarTestStatusYCLZT("82", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "82发送成功", "SDSM上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "82发送失败", "SDSM上传过程", 1);
                        //更新YCLZT到82
                    }
                    if (zt == "6" && yclzt == "82")
                    {
                        string test_result = "";
                        if (UpdateResult("SDS",jylsh, jycs, out test_result))
                        {
                            FileOpreate.SaveLog("流水号：" + jylsh + "结果数据发送成功", "SDSM上传过程", 1);
                            if (UpdateProjectStop(dt_test_status.Rows[0], test_result))
                            {
                                UpdateStatus(jylsh, jycs, "已处理状态", "上传完毕");
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目结束命令发送成功", "SDSM上传过程", 1);
                                //更新其他状态
                                DataRow dr = dt_AlreadyUpload.NewRow();
                                dr["检验流水号"] = dt_test_status.Rows[0]["JYLSH"].ToString();
                                dr["车辆号牌"] = dt_test_status.Rows[0]["CLHP"].ToString();
                                dr["号牌种类"] = dt_test_status.Rows[0]["HPZL"].ToString();
                                dr["线号"] = dt_test_status.Rows[0]["LINEID"].ToString();
                                dr["检验次数"] = dt_test_status.Rows[0]["JYCS"].ToString();
                                dr["检验时间"] = dt_test_status.Rows[0]["JCSJ"].ToString();
                                dr["检测方法"] = dt_test_status.Rows[0]["JCFF"].ToString();
                                dr["上传成功时间"] = System.DateTime.Now.ToString();
                                dt_AlreadyUpload.Rows.Add(dr);
                                FileOpreate.SaveLog("流水号：" + jylsh + "所有数据上传完毕", "SDSM上传过程", 1);
                                carinfodb.DeleteCarTestStatus(jylsh, jycs);
                                carinfodb.deleteCarInWaitlist(jylsh, jycs);
                            }
                            else
                            {
                                carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, test_result);//将该条记录置为上传失败记录
                                FileOpreate.SaveLog("流水号：" + jylsh + "检测结束上传失败", "SDSM上传过程", 1);
                            }
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, test_result);//将该条记录置为上传失败记录
                            FileOpreate.SaveLog("流水号：" + jylsh + "上传失败", "SDSM上传过程", 1);
                        }
                        //更新YCLZT到6
                    }

                    //更新YCLZT状态到实际执行到的位置
                    //carinfodb.UpdateCarTestStatusYCLZT(yclzt, jylsh, jycs);
                    UpdateStatus(jylsh, jycs, "备注", "当前数据上传完");
                }
                catch (Exception er)
                {
                    //更新YCLZT状态到实际执行到的位置
                    carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, er.Message);
                    FileOpreate.SaveLog("数据上传错误原因：" + er.Message, "SDS上传错误", 1);
                }
            }
            catch (Exception er)
            {
                //carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, er.Message);
                FileOpreate.SaveLog("线程运行错误原因：" + er.Message, "ASM上传线程错误", 1);
            }
        }
        private void UploadJZJSData(object obj)
        {
            try
            {
                DataTable dt_test_status = ((DataTable)obj).Copy();

                if (dt_test_status.Rows.Count <= 0)
                {
                    //carinfodb.UpdateCarTestStatusYCLZT(dr_temp["YCLZT"].ToString(), dr_temp["JYLSH"].ToString(), dr_temp["JYCS"].ToString());
                    FileOpreate.SaveLog("JZJS上传线程传入数据为空" + obj.ToString(), "JZJS上传线程执行失败", 1);
                    return;
                }
                string jylsh = dt_test_status.Rows[0]["JYLSH"].ToString();
                string jycs = dt_test_status.Rows[0]["JYCS"].ToString();
                string zt = dt_test_status.Rows[0]["ZT"].ToString();
                string yclzt = dt_test_status.Rows[0]["YCLZT"].ToString();

                try
                {
                    FileOpreate.SaveLog("流水号：" + jylsh + "JZJS数据上传准备", "JZJS上传过程", 1);
                    if (zt == "1")
                    {
                        //发请求开始——KS001，解析开始是否同意，更新检测状态，并更新YCLZT到1
                        string error_info = "";
                        if (UpdateProjectStart(dt_test_status.Rows[0], out error_info))
                        {
                            if (carinfodb.UpdateCarTestStatusZT("2", jylsh, jycs, error_info))
                            {
                                yclzt = "2";
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目开始发送成功", "JZJS上传过程", 1);
                            }
                            else
                                FileOpreate.SaveLog("流水号：" + jylsh + "更新允许开始状态失败", "JZJS上传过程", 1);
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusZT("3", jylsh, jycs, error_info);
                            carinfodb.UpdateCarTestStatusYCLZT("0", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "不允许开始，原因：" + error_info, "JZJS上传过程", 1);
                            return;
                        }
                    }
                    if ((zt == "41" || zt == "42" || zt == "43" || zt == "6") && yclzt == "2")
                    {
                        //发41照片
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "41"))
                        {
                            //yclzt = "41";
                            carinfodb.UpdateCarTestStatusYCLZT("41", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "41发送成功", "JZJS上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "41发送失败", "JZJS上传过程", 1);
                        //更新YCLZT到41
                    }
                    if ((zt == "42" || zt == "43" || zt == "6") && yclzt == "41")
                    {
                        //发42
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "42"))
                        {
                            //yclzt = "42";
                            carinfodb.UpdateCarTestStatusYCLZT("42", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "42发送成功", "JZJS上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "42发送失败", "JZJS上传过程", 1);
                        //更新YCLZT到42
                    }
                    if ((zt == "43" || zt == "6") && yclzt == "42")
                    {
                        //发43
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "43"))
                        {
                           // yclzt = "43";
                            carinfodb.UpdateCarTestStatusYCLZT("43", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "43发送成功", "JZJS上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "43发送失败", "JZJS上传过程", 1);
                        //更新YCLZT到43
                    }
                    if (zt == "6" && yclzt == "43")
                    {
                        //发过程数据、结果数据及检测结束命令——JS001

                        string test_result = "";
                        if (UpdateResult("JZJS",jylsh, jycs, out test_result))
                        {
                            FileOpreate.SaveLog("流水号：" + jylsh + "结果数据发送成功", "JZJS上传过程", 1);
                            if (UpdateProjectStop(dt_test_status.Rows[0], test_result))
                            {
                                UpdateStatus(jylsh, jycs, "已处理状态", "上传完毕");
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目结束命令发送成功，所有数据上传完毕", "JZJS上传过程", 1);
                                //更新其他状态
                                DataRow dr = dt_AlreadyUpload.NewRow();
                                dr["检验流水号"] = dt_test_status.Rows[0]["JYLSH"].ToString();
                                dr["车辆号牌"] = dt_test_status.Rows[0]["CLHP"].ToString();
                                dr["号牌种类"] = dt_test_status.Rows[0]["HPZL"].ToString();
                                dr["线号"] = dt_test_status.Rows[0]["LINEID"].ToString();
                                dr["检验次数"] = dt_test_status.Rows[0]["JYCS"].ToString();
                                dr["检验时间"] = dt_test_status.Rows[0]["JCSJ"].ToString();
                                dr["检测方法"] = dt_test_status.Rows[0]["JCFF"].ToString();
                                dr["上传成功时间"] = System.DateTime.Now.ToString();
                                dt_AlreadyUpload.Rows.Add(dr);
                                carinfodb.DeleteCarTestStatus(jylsh, jycs);
                                carinfodb.deleteCarInWaitlist(jylsh, jycs);
                            }
                            else
                            {
                                carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, test_result);//将该条记录置为上传失败记录
                                FileOpreate.SaveLog("流水号：" + jylsh + "检测结束上传失败", "JZJS上传过程", 1);
                            }
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, test_result);//将该条记录置为上传失败记录
                            FileOpreate.SaveLog("流水号：" + jylsh + "上传失败", "JZJS上传过程", 1);
                        }
                        //更新YCLZT到6
                    }

                    //更新YCLZT状态到实际执行到的位置
                    //carinfodb.UpdateCarTestStatusYCLZT(yclzt, jylsh, jycs);
                    UpdateStatus(jylsh, jycs, "备注", "当前数据上传完");
                }
                catch (Exception er)
                {
                    //更新YCLZT状态到实际执行到的位置
                    carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, er.Message);
                    FileOpreate.SaveLog("数据上传错误原因：" + er.Message, "JZJS上传错误", 1);
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("线程运行错误原因：" + er.Message, "JZJS上传线程错误", 1);
            }
        }
        private void UploadZYJSData(object obj)
        {
            try
            {
                DataTable dt_test_status = ((DataTable)obj).Copy();

                if (dt_test_status.Rows.Count <= 0)
                {
                    //carinfodb.UpdateCarTestStatusYCLZT(dr_temp["YCLZT"].ToString(), dr_temp["JYLSH"].ToString(), dr_temp["JYCS"].ToString());
                    FileOpreate.SaveLog("ZYJS上传线程传入数据为空" + obj.ToString(), "ZYJS上传线程执行失败", 1);
                    return;
                }
                string jylsh = dt_test_status.Rows[0]["JYLSH"].ToString();
                string jycs = dt_test_status.Rows[0]["JYCS"].ToString();
                string zt = dt_test_status.Rows[0]["ZT"].ToString();
                string yclzt = dt_test_status.Rows[0]["YCLZT"].ToString();
                try
                {
                    FileOpreate.SaveLog("流水号：" + jylsh + "ZYJS数据上传准备", "ZYJS上传过程", 1);
                    if (zt == "1")
                    {
                        //发请求开始——KS001，解析开始是否同意，更新检测状态，并更新YCLZT到1
                        string error_info = "";
                        if (UpdateProjectStart(dt_test_status.Rows[0], out error_info))
                        {
                            if (carinfodb.UpdateCarTestStatusZT("2", jylsh, jycs, error_info))
                            {
                                carinfodb.UpdateCarTestStatusYCLZT("2", jylsh, jycs);
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目开始发送成功", "ZYJS上传过程", 1);
                            }
                            else
                            {
                                FileOpreate.SaveLog("流水号：" + jylsh + "更新允许开始状态失败", "ZYJS上传过程", 1);
                            }
                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusZT("3", jylsh, jycs, error_info);
                            carinfodb.UpdateCarTestStatusYCLZT("0", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "不允许开始，原因：" + error_info, "ZYJS上传过程", 1);
                            return;
                        }
                    }
                    if ((zt == "51" || zt == "52" || zt == "53" || zt == "6") && yclzt == "2")
                    {
                        //发51照片
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "51"))
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("51", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "51发送成功", "ZYJS上传过程", 1);
                        }
                        else
                        {
                            FileOpreate.SaveLog("流水号：" + jylsh + "51发送失败", "ZYJS上传过程", 1);
                        }
                        //更新YCLZT到51
                    }
                    if ((zt == "52" || zt == "53" || zt == "6") && yclzt == "51")
                    {
                        //发52
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "52"))
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("52", jylsh, jycs);
                            FileOpreate.SaveLog("流水号：" + jylsh + "52发送成功", "ZYJS上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "52发送失败", "ZYJS上传过程", 1);
                        //更新YCLZT到52
                    }
                    if ((zt == "53" || zt == "6") && yclzt == "52")
                    {
                        //发53
                        if (UpdateCapturePicture(dt_test_status.Rows[0], "53"))
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("53", jylsh, jycs);
                            //yclzt = "53";
                            FileOpreate.SaveLog("流水号：" + jylsh + "53发送成功", "ZYJS上传过程", 1);
                        }
                        else
                            FileOpreate.SaveLog("流水号：" + jylsh + "53发送失败", "ZYJS上传过程", 1);
                        //更新YCLZT到53
                    }
                    if (zt == "6" && yclzt == "53")
                    {
                        string test_result = "";
                        if (UpdateResult("ZYJS", jylsh, jycs, out test_result))
                        {
                            FileOpreate.SaveLog("流水号：" + jylsh + "结果数据发送成功", "ZYJS上传过程", 1);
                            if (UpdateProjectStop(dt_test_status.Rows[0], test_result))
                            {
                                UpdateStatus(jylsh, jycs, "已处理状态", "上传完毕");
                                FileOpreate.SaveLog("流水号：" + jylsh + "项目结束发送成功，所有数据上传完毕", "ZYJS上传过程", 1);
                                //更新其他状态
                                DataRow dr = dt_AlreadyUpload.NewRow();
                                dr["检验流水号"] = dt_test_status.Rows[0]["JYLSH"].ToString();
                                dr["车辆号牌"] = dt_test_status.Rows[0]["CLHP"].ToString();
                                dr["号牌种类"] = dt_test_status.Rows[0]["HPZL"].ToString();
                                dr["线号"] = dt_test_status.Rows[0]["LINEID"].ToString();
                                dr["检验次数"] = dt_test_status.Rows[0]["JYCS"].ToString();
                                dr["检验时间"] = dt_test_status.Rows[0]["JCSJ"].ToString();
                                dr["检测方法"] = dt_test_status.Rows[0]["JCFF"].ToString();
                                dr["上传成功时间"] = System.DateTime.Now.ToString();
                                dt_AlreadyUpload.Rows.Add(dr);
                                carinfodb.DeleteCarTestStatus(jylsh, jycs);
                                carinfodb.deleteCarInWaitlist(jylsh, jycs);
                            }
                            else
                            {
                                carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs, test_result);
                                FileOpreate.SaveLog("流水号：" + jylsh + "检测结束上传失败", "ZYJS上传过程", 1);
                            }

                        }
                        else
                        {
                            carinfodb.UpdateCarTestStatusYCLZT("-1", jylsh, jycs,test_result);
                            FileOpreate.SaveLog("流水号：" + jylsh + "过程数据上传失败", "ZYJS上传过程", 1);
                        }
                        //更新YCLZT到6
                    }

                    //更新YCLZT状态到实际执行到的位置
                    carinfodb.UpdateCarTestStatusYCLZT(yclzt, jylsh, jycs);
                    UpdateStatus(jylsh, jycs, "备注", "当前数据上传完");
                }
                catch (Exception er)
                {
                    //更新YCLZT状态到实际执行到的位置
                    carinfodb.UpdateCarTestStatusYCLZT(yclzt, jylsh, jycs);
                    FileOpreate.SaveLog("数据上传错误原因：" + er.Message, "ZYJS上传错误", 1);
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("线程运行错误原因：" + er.Message, "ZYJS上传线程错误", 1);
            }
        }
        #endregion

        #region 上报数据发送操作，以流水号及检验测试查询数据
        //项目开始发送
        private bool UpdateProjectStart(DataRow TestStatus, out string error_info)
        {
            error_info = "";
            return true;
        }
        
        //项目结束发送
        private bool UpdateProjectStop(DataRow TestStatus, string test_result)
        {

            test_result = "";
            return true;
        }
        
        //项目照片发送
        private bool UpdateCapturePicture(DataRow TestStatus, string photo_num)
        {
            photo_num = "";
            return true;
        }
        
        //瞬态检测结果发送
        private bool UpdateResult(string jcff,string jylsh, string jycs, out string test_result)
        {
            test_result = "";
            try
            {
                DataRow obj_result = null;
                DataRow obj_process = null;
                DataRow obj_car = null;
                carinfodb.getTestResult(jcff=="ZYJS"?"Zyjs_Btg":jcff, jylsh, jycs, out obj_result);
                carinfodb.getTestResult(jcff+"_DATASECONDS", jylsh, jycs, out obj_process);
                carinfodb.getTestResult("已检车辆信息", jylsh, jycs, out obj_car);
                if (obj_result==null)
                {
                    test_result = "没有找到结果数据";
                    FileOpreate.SaveLog("没有找到结果数据", "["+jylsh+"]上传过程错误", 1);
                    return false;
                }
                if (obj_process == null )
                {
                    test_result = "没有找到过程数据";
                    FileOpreate.SaveLog("没有找到过程数据", "[" + jylsh + "]上传过程错误", 1);
                    return false;
                }
                if ( obj_car == null)
                {
                    test_result = "没有找到车辆数据";
                    FileOpreate.SaveLog("没有找到车辆数据", "[" + jylsh + "]上传过程错误", 1);
                    return false;
                }
                string clid = obj_car["CLID"].ToString();
                bool isOBDChecked = (obj_car["OBD"].ToString() == "Y" || obj_car["OBD"].ToString() == "有" || obj_car["OBD"].ToString() == "是" || obj_car["OBD"].ToString() == "1");
                if(isOBDChecked)
                {
                    DataRow obj_obdresult = null;
                    carinfodb.getOBDResult(clid, out obj_obdresult);
                    if (obj_obdresult == null)
                    {
                        test_result = "没有找到OBD数据";
                        FileOpreate.SaveLog("没有找到OBD数据", "[" + jylsh + "]上传过程错误", 1);
                        return false;
                    }
                    string obdresultid = "";
                    if(!interfaceUploadJh.uploadOBDResult(obj_car,obj_obdresult,out obdresultid))
                    {
                        test_result = "发送OBD检查数据失败";
                        FileOpreate.SaveLog("发送OBD检查数据失败", "[" + jylsh + "]上传过程错误", 1);
                        return false;
                    }
                    FileOpreate.SaveLog("发送OBD检查数据成功", "[" + jylsh + "]上传过程", 1);
                    if (!interfaceUploadJh.uploadOBDProcess(obj_car, obj_process,obdresultid))
                    {
                        test_result = "发送OBD过程数据失败";
                        FileOpreate.SaveLog("发送OBD过程数据失败", "[" + jylsh + "]上传过程错误", 1);
                        return false;
                    }
                    FileOpreate.SaveLog("发送OBD过程数据成功", "[" + jylsh + "]上传过程", 1);
                    if (!interfaceUploadJh.uploadOBDIUPR(obj_car, obj_obdresult, obdresultid))
                    {
                        test_result = "发送OBD IUPR数据失败";
                        FileOpreate.SaveLog("发送OBD IUPR数据失败", "[" + jylsh + "]上传过程错误", 1);
                        return false;
                    }
                    FileOpreate.SaveLog("发送OBD IUPR数据成功", "[" + jylsh + "]上传过程", 1);
                }
                else
                {
                    FileOpreate.SaveLog("未进行OBD检测，不用上传OBD检测", "[" + jylsh + "]上传过程", 1);
                }
                string testingid = "";
                if (!interfaceUploadJh.uploadExhaustResult(obj_car, obj_result, out testingid))
                {
                    test_result = "发送"+jcff+"结果数据失败";
                    FileOpreate.SaveLog("发送" + jcff + "结果数据失败", "[" + jylsh + "]上传过程错误", 1);
                    return false;
                }
                FileOpreate.SaveLog("发送VMAS结果数据成功", "VMAS结果数据发送", 1);
                if (!interfaceUploadJh.uploadExhaustProcess(obj_car, obj_process,testingid))
                {
                    test_result = "发送" + jcff + "过程数据失败";
                    FileOpreate.SaveLog("发送" + jcff + "过程数据失败", "[" + jylsh + "]上传过程错误", 1);
                    return false;
                }
                FileOpreate.SaveLog("发送" + jcff + "过程数据成功", "[" + jylsh + "]上传过程", 1);
                return true;

            }
            catch (Exception er)
            {

                test_result = "上传过程异常"+er.Message;
                FileOpreate.SaveLog("执行" + jcff + "结果数据发送失败，原因：" + er.Message, "[" + jylsh + "]上传过程异常", 1);
                return false;
            }
        }
        
       
        #endregion
        #region 设备标定数据上传
        private void UpdateBD(object BDData)
        {
            try
            {
                DataRow temp_dr = (DataRow)BDData;
                DataTable temp_dt_bddata = temp_dr.Table.Clone();
                temp_dt_bddata.ImportRow(temp_dr);
                string lineid = temp_dt_bddata.Rows[0]["JCGWH"].ToString();
                int uploadTimes = int.Parse(temp_dt_bddata.Rows[0]["BY1"].ToString());
                string id = temp_dt_bddata.Rows[0]["ID"].ToString();
                string lx = temp_dt_bddata.Rows[0]["LX"].ToString();
                try
                {
                    if (interfaceUploadJh.uploadInspStmDeviceRecord(temp_dt_bddata.Rows[0]))
                    {
                        FileOpreate.SaveLog("【设备标定数据】|ID=" + id + "|LX=" + lx + "|数据上传成功", "标定数据发送", 1);
                        Update_ZjBd_Log(lineid, int.Parse(temp_dt_bddata.Rows[0]["LX"].ToString()), "已上传");
                        carinfodb.UpdateBDZT("1", lx, 0);
                    }
                    else
                    {
                        FileOpreate.SaveLog("！！！！【设备标定数据】|ID=" + id + "|LX=" + lx + "|数据上传失败！！！！", "标定数据发送", 1);
                        //上传不成功则将状态改回0
                        carinfodb.UpdateBDZT("0", lx, ++uploadTimes);
                    }
                }
                catch (Exception er)
                {
                    FileOpreate.SaveLog("！！！！【设备标定数据】|ID=" + id + "|LX=" + lx + "|数据上传发生异常：" + er.Message + "！！！！", "标定数据发送", 1);
                    carinfodb.UpdateBDZT("0", lx, ++uploadTimes);
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("！！！！【设备标定数据】|取标定数据发生异常：" + er.Message + "！！！！", "标定数据发送", 1);
            }
        }
        #endregion

        #region 设备自检数据上传
        private void UpdateZj(object BDData)
        {
            try
            {
                DataRow temp_dr = (DataRow)BDData;
                DataTable temp_dt_bddata = temp_dr.Table.Clone();
                temp_dt_bddata.ImportRow(temp_dr);
                string lineid = temp_dt_bddata.Rows[0]["JCGWH"].ToString();
                int uploadTimes = int.Parse(temp_dt_bddata.Rows[0]["BY1"].ToString());
                string id = temp_dt_bddata.Rows[0]["ID"].ToString();
                string lx = temp_dt_bddata.Rows[0]["LX"].ToString();
                try
                {
                    if (interfaceUploadJh.uploadInspStmDeviceRecord(temp_dt_bddata.Rows[0]))
                    {
                        FileOpreate.SaveLog("【设备自检数据】|ID=" + id + "|LX=" + lx + "|数据上传成功", "标定数据发送", 1);
                        Update_ZjBd_Log(lineid, int.Parse(temp_dt_bddata.Rows[0]["LX"].ToString()), "已上传");
                        carinfodb.UpdateSelfCheckZT("1", lx, 0);
                    }
                    else
                    {
                        FileOpreate.SaveLog("！！！！【设备自检数据】|ID=" + id + "|LX=" + lx + "|数据上传失败！！！！", "标定数据发送", 1);
                        //上传不成功则将状态改回0
                        carinfodb.UpdateSelfCheckZT("0", lx, ++uploadTimes);
                    }
                }
                catch (Exception er)
                {
                    FileOpreate.SaveLog("！！！！【设备自检数据】|ID=" + id + "|LX=" + lx + "|数据上传发生异常：" + er.Message + "！！！！", "标定数据发送", 1);
                    carinfodb.UpdateSelfCheckZT("0", lx, ++uploadTimes);
                }
            }
            catch (Exception er)
            {
                FileOpreate.SaveLog("！！！！【设备自检数据】|取标定数据发生异常：" + er.Message + "！！！！", "标定数据发送", 1);
            }
        }
        #endregion
        
        private void 标定自检上传记录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataTable show_temp = dt_ZjBdUpload.Copy();
            if (FileOpreate.Interface_Area == 0)
            {
                //修改自检标定列名称
                show_temp.Columns[1].ColumnName = "加载滑行";
                show_temp.Columns[2].ColumnName = "废气仪自检";
                show_temp.Columns[3].ColumnName = "烟度计自检";
                show_temp.Columns[4].ColumnName = "电子环境信息仪自检";
                show_temp.Columns[5].ColumnName = "发动机转速仪自检";
                show_temp.Columns[6].ColumnName = "流量计自检";
                show_temp.Columns[7].ColumnName = "寄生功率";
                show_temp.Columns[8].ColumnName = "车速标定";
                show_temp.Columns[9].ColumnName = "扭力标定";
                show_temp.Columns[10].ColumnName = "寄生功率标定";
                show_temp.Columns[11].ColumnName = "加载滑行标定";
                show_temp.Columns[12].ColumnName = "废气标定";
                show_temp.Columns[13].ColumnName = "烟度标定";
                show_temp.Columns[14].ColumnName = "惯量标定";
                show_temp.Columns[15].ColumnName = "响应时间标定";
            }
            else if (FileOpreate.Interface_Area == 1 || FileOpreate.Interface_Area == 2)
            {
                //移除不需要的列
                show_temp.Columns.RemoveAt(15);
                show_temp.Columns.RemoveAt(14);
                //修改自检标定列名称
                show_temp.Columns[1].ColumnName = "加载滑行检查";
                show_temp.Columns[2].ColumnName = "附加功率损失检查";
                show_temp.Columns[3].ColumnName = "分析仪检查";
                show_temp.Columns[4].ColumnName = "泄露检查";
                show_temp.Columns[5].ColumnName = "分析仪氧量程检查";
                show_temp.Columns[6].ColumnName = "低标气检查";
                show_temp.Columns[7].ColumnName = "流量计检查";
                show_temp.Columns[8].ColumnName = "车速校准";
                show_temp.Columns[9].ColumnName = "扭力校准";
                show_temp.Columns[10].ColumnName = "寄生功率校准";
                show_temp.Columns[11].ColumnName = "加载滑行校准";
                show_temp.Columns[12].ColumnName = "废气仪校准";
                show_temp.Columns[13].ColumnName = "烟度计校准";
            }
            else
            {
                //移除不需要的列
                show_temp.Columns.RemoveAt(15);
                //修改自检标定列名称
                show_temp.Columns[1].ColumnName = "加载滑行检查";
                show_temp.Columns[2].ColumnName = "附加功率损失检查";
                show_temp.Columns[3].ColumnName = "分析仪检查";
                show_temp.Columns[4].ColumnName = "泄露检查";
                show_temp.Columns[5].ColumnName = "分析仪氧量程检查";
                show_temp.Columns[6].ColumnName = "低标气检查";
                show_temp.Columns[7].ColumnName = "流量计检查";
                show_temp.Columns[8].ColumnName = "电子环境信息检查";
                show_temp.Columns[9].ColumnName = "车速校准";
                show_temp.Columns[10].ColumnName = "扭力校准";
                show_temp.Columns[11].ColumnName = "寄生功率校准";
                show_temp.Columns[12].ColumnName = "加载滑行校准";
                show_temp.Columns[13].ColumnName = "废气仪校准";
                show_temp.Columns[14].ColumnName = "烟度计校准";
            }
            dataGridView_Ready_And_Failed.Visible = false;
            dataGridView_AlreadyUpload.Visible = false;
            dataGridView_BDZJ.Visible = true;
            dataGridView_BDZJ.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            //显示标定自检上传记录表
            dataGridView_BDZJ.DataSource = show_temp;

            tSSL_Status.Text = "状态：当前显示标定自检数据上传记录！";
        }

        private void 数据库升级ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //alter table [车辆检测状态] alter column BZ varchar(max)
            if (carinfodb.updateSQL())
                MessageBox.Show("数据库升级成功！");
            else
                MessageBox.Show("数据库升级失败！");
        }
        #region 其他功能函数
        /// <summary>
        /// 初始化状态代码
        /// </summary>
        private void InitJcZt()
        {
            JcZt.Add("-1", "上传失败");
            JcZt.Add("", "空状态");
            JcZt.Add("0", "检测准备");
            JcZt.Add("1", "请求开始");
            JcZt.Add("2", "允许检测");
            JcZt.Add("3", "不允许检测");
            JcZt.Add("4", "开始检测");
            JcZt.Add("5", "未完成退出");
            JcZt.Add("6", "检测完成");
            JcZt.Add("11", "SDS高怠速中");
            JcZt.Add("12", "SDS低怠速中");
            JcZt.Add("21", "5025开始");
            JcZt.Add("22", "ASM5025结束");
            JcZt.Add("23", "ASM2540开始");
            JcZt.Add("24", "ASM2540结束");
            JcZt.Add("31", "Vmas15km/h中");
            JcZt.Add("32", "Vmas32km/h中");
            JcZt.Add("33", "Vmas50km/h中");
            JcZt.Add("41", "JZJS100Vel");
            JcZt.Add("42", "JZJS90Vel");
            JcZt.Add("43", "JZJS80Vel");
            JcZt.Add("51", "ZYJS第一次");
            JcZt.Add("52", "ZYJS第二次");
            JcZt.Add("53", "ZYJS第三次");
        }

        private void Update_ZjBd_Log(string line_id, int xm_index, string result)
        {
            DataRow[] dr_change_temp = dt_ZjBdUpload.Select("线号='" + line_id + "'");
            if (dr_change_temp.Length > 0)
                dr_change_temp[0][xm_index] = result;
        }
        #endregion

        private void 上传失败记录ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            dataGridView_UploadFailed.Visible = true;
            dataGridView_Ready_And_Failed.Visible = false;
            tSSL_Status.Text = "状态：当前显示上传失败数据列表！";
        }
        /// <summary>
        /// 刷新失败列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerRefreshFaileData_Tick(object sender, EventArgs e)
        {
            if (!_whether_Connected)//若同步时间失败则不上传
                return;

            try
            {
                DataTable CarTestStatusTable = carinfodb.getCarUploadFailed();//获取车辆检测状态表
                #region 显示 
                dt_UploadFailed.Clear();
                if (CarTestStatusTable != null && CarTestStatusTable.Rows.Count > 0)
                {
                    for (int i = 0; i < CarTestStatusTable.Rows.Count; i++)
                    {
                        DataRow dr = dt_UploadFailed.NewRow();
                        dr["检验流水号"] = CarTestStatusTable.Rows[i]["JYLSH"].ToString();
                        dr["车辆号牌"] = CarTestStatusTable.Rows[i]["CLHP"].ToString();
                        dr["号牌种类"] = CarTestStatusTable.Rows[i]["HPZL"].ToString();
                        dr["线号"] = CarTestStatusTable.Rows[i]["LINEID"].ToString();
                        dr["检验次数"] = CarTestStatusTable.Rows[i]["JYCS"].ToString();
                        dr["检验时间"] = CarTestStatusTable.Rows[i]["JCSJ"].ToString();
                        dr["检测方法"] = CarTestStatusTable.Rows[i]["JCFF"].ToString();
                        dr["当前状态"] = JcZt[CarTestStatusTable.Rows[i]["ZT"].ToString()];
                        dr["已处理状态"] = CarTestStatusTable.Rows[i]["YCLZT"].ToString() == "6" ? "上传过程/结果数据" : JcZt[CarTestStatusTable.Rows[i]["YCLZT"].ToString()];
                        dr["备注"] = CarTestStatusTable.Rows[i]["BZ"].ToString();
                        dt_UploadFailed.Rows.Add(dr);
                    }
                }
                dataGridView_UploadFailed.DataSource = dt_UploadFailed;
                #endregion
            }
            catch (Exception er)
            {
            }
        }
    }

    #region 设置系统时间
    public class SetSystemDateTime
    {
        [DllImport("Kernel32.dll")]
        public static extern bool SetLocalTime(ref SystemTime sysTime);

        public static bool SetLocalTimeByStr(string timestr)
        {
            bool flag = false;
            SystemTime sysTime = new SystemTime();
            DateTime dt = Convert.ToDateTime(timestr);
            sysTime.wYear = Convert.ToUInt16(dt.Year);
            sysTime.wMonth = Convert.ToUInt16(dt.Month);
            sysTime.wDay = Convert.ToUInt16(dt.Day);
            sysTime.wHour = Convert.ToUInt16(dt.Hour);
            sysTime.wMinute = Convert.ToUInt16(dt.Minute);
            sysTime.wSecond = Convert.ToUInt16(dt.Second);
            sysTime.wMiliseconds = Convert.ToUInt16(dt.Millisecond);
            try
            {
                flag = SetSystemDateTime.SetLocalTime(ref sysTime);
            }
            catch (Exception e)
            {
                FileOpreate.SaveLog("SetSystemDateTime函数执行异常" + e.Message,"SetSystemTimeError", 1);
            }
            return flag;
        }
    }
    public struct SystemTime
    {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMiliseconds;
    }
    #endregion
}