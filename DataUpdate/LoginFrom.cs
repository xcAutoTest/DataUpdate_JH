using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataUpdate
{
    public partial class LoginFrom : Form
    {
        private bool login_status;//指示是否登录成功
        public bool LoginStatus { get { return login_status; } }
        private DBControl userinfodb = new DBControl();//定义数据库操作

        public LoginFrom()
        {
            InitializeComponent();
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            panel_login.Location = new Point(51, 90);
            panel_config.Location = new Point(51, 90);
            panel_config.Visible = false;

            if (FileOpreate.SaveUser)
            {
                textBox_User_Name.Text = FileOpreate.User_Name;
                checkBox_save_name.Checked = true;
            }
            if (FileOpreate.SavePwd)
            {
                textBox_User_Pwd.Text = FileOpreate.User_Pwd;
                checkBox_save_pwd.Checked = true;
            }
        }

        private void button_change_config_Click(object sender, EventArgs e)
        {
            panel_login.Visible = false;
            panel_config.Visible = true;
            //数据库配置
            textBox_DB_address.Text = FileOpreate.DB_Address;
            textBox_DB_name.Text = FileOpreate.DB_Name;
            textBox_DB_user.Text = FileOpreate.DB_User;
            textBox_DB_pwd.Text = FileOpreate.DB_Pwd;
            //联网接口配置
            textBox_Interface_address.Text = FileOpreate.Interface_Address;
            textBox_Interface_jkxlh.Text = FileOpreate.Interface_Jkxlh;
            textBox_Interface_jczid.Text = FileOpreate.Interface_JczID;
            comboBox_Interface_Area.SelectedIndex = FileOpreate.Interface_Area;
            textBox_NM_sqr.Text = FileOpreate.NM_Sqr;
            textBox_NM_zlkzr.Text = FileOpreate.NM_Zlkzr;
            //联网上传设置
            comboBox_data_update_style.SelectedIndex = FileOpreate.UpdateStyle;
            comboBox_update_fail_solve.SelectedIndex = FileOpreate.UpdateFailSovle;
            numericUpDown_SetTimeOfGetWaitList.Value = FileOpreate.UpdateGetWaitListTime;
            checkBox_getCarListUsed.Checked = FileOpreate.UpdateAutoGetCarList == 1 ? true : false;
                        
            //检测方法
            checkBox14.Checked = FileOpreate.jcff.sds;
            checkBox15.Checked = FileOpreate.jcff.asm;
            checkBox16.Checked = FileOpreate.jcff.vmas;
            checkBox17.Checked = FileOpreate.jcff.jzjs;
            checkBox18.Checked = FileOpreate.jcff.zyjs;
        }

        //退出数据库配置保存
        private void button_Exit_Config_Click(object sender, EventArgs e)
        {
            panel_login.Visible = true;
            panel_config.Visible = false;
        }

        //保存数据库配置
        private void button_SaveDbConfig_Click(object sender, EventArgs e)
        {
            try
            {
                FileOpreate.DB_Address = textBox_DB_address.Text;
                FileOpreate.DB_Name = textBox_DB_name.Text;
                FileOpreate.DB_User = textBox_DB_user.Text;
                FileOpreate.DB_Pwd = textBox_DB_pwd.Text;
                FileOpreate.WritePrivateProfileString("数据库", "服务器", textBox_DB_address.Text, @"./Config.ini");
                FileOpreate.WritePrivateProfileString("数据库", "数据库名", textBox_DB_name.Text, @"./Config.ini");
                FileOpreate.WritePrivateProfileString("数据库", "用户名", textBox_DB_user.Text, @"./Config.ini");
                FileOpreate.WritePrivateProfileString("数据库", "密码", textBox_DB_pwd.Text, @"./Config.ini");
                MessageBox.Show("数据库信息保存成功！", "保存成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show("原因：" + ex.Message.ToString(), "数据库配置信息保存失败");
            }
        }

        //保存接口配置
        private void button_SaveInterfaceConfig_Click(object sender, EventArgs e)
        {
            try
            {
                if (FileOpreate.Interface_Area == 2 || FileOpreate.Interface_Area == 3)
                {
                    if (textBox_NM_sqr.Text == "" || textBox_NM_zlkzr.Text == "")
                    {
                        MessageBox.Show("当前联网下必须输入授权人及质量控制员姓名！", "警告：");
                        return;
                    }
                    else
                    {
                        FileOpreate.NM_Sqr = textBox_NM_sqr.Text;
                        FileOpreate.NM_Zlkzr = textBox_NM_zlkzr.Text;
                        FileOpreate.WritePrivateProfileString("用户", "内蒙联授权人", FileOpreate.NM_Sqr, @"./Config.ini");
                        FileOpreate.WritePrivateProfileString("用户", "内蒙联网质量控制人", FileOpreate.NM_Zlkzr, @"./Config.ini");
                    }
                }
                FileOpreate.Interface_Area = comboBox_Interface_Area.SelectedIndex;
                FileOpreate.Interface_Address = textBox_Interface_address.Text;
                FileOpreate.Interface_Jkxlh = textBox_Interface_jkxlh.Text;
                FileOpreate.Interface_JczID = textBox_Interface_jczid.Text;
                FileOpreate.WritePrivateProfileString("联网信息", "联网地区", comboBox_Interface_Area.SelectedIndex.ToString(), @"./Config.ini");
                FileOpreate.WritePrivateProfileString("联网信息", "接口地址", textBox_Interface_address.Text, @"./Config.ini");
                FileOpreate.WritePrivateProfileString("联网信息", "接口序列号", textBox_Interface_jkxlh.Text, @"./Config.ini");
                FileOpreate.WritePrivateProfileString("联网信息", "检测站ID", textBox_Interface_jczid.Text, @"./Config.ini");
                MessageBox.Show("联网信息保存成功！", "保存成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show("原因：" + ex.Message.ToString(), "接口配置保存错误");
            }
        }

        //退出接口配置保存
        private void button1_Click(object sender, EventArgs e)
        {
            panel_login.Visible = true;
            panel_config.Visible = false;
        }

        //保存软件上传设置
        private void button_SaveUpdateSet_Click(object sender, EventArgs e)
        {
            try
            {
                FileOpreate.UpdateStyle = comboBox_data_update_style.SelectedIndex;
                FileOpreate.UpdateFailSovle = comboBox_update_fail_solve.SelectedIndex;
                FileOpreate.UpdateGetWaitListTime = (int)numericUpDown_SetTimeOfGetWaitList.Value;
                FileOpreate.UpdateAutoGetCarList = checkBox_getCarListUsed.Checked ? 1 : 0;
                FileOpreate.WritePrivateProfileString("软件配置", "上传方式", FileOpreate.UpdateStyle.ToString(), @"./Config.ini");
                FileOpreate.WritePrivateProfileString("软件配置", "上传失败处理方式", FileOpreate.UpdateFailSovle.ToString(), @"./Config.ini");
                FileOpreate.WritePrivateProfileString("软件配置", "待检列表刷新间隔", FileOpreate.UpdateGetWaitListTime.ToString(), @"./Config.ini");
                FileOpreate.WritePrivateProfileString("软件配置", "是否开启自动获取待检列表", FileOpreate.UpdateAutoGetCarList.ToString(), @"./Config.ini");
                MessageBox.Show("接口配置信息保存成功！", "保存成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show("原因：" + ex.Message, "软件配置保存失败");
            }
        }

        //退出上传设置配置保存
        private void button3_Click(object sender, EventArgs e)
        {
            panel_login.Visible = true;
            panel_config.Visible = false;
        }
        private void checkBox_save_name_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_save_name.Checked)
            {
                if (textBox_User_Name.Text != "")
                {
                    if (textBox_User_Name.Text != FileOpreate.User_Name)
                    {
                        FileOpreate.User_Name = textBox_User_Name.Text;
                        FileOpreate.WritePrivateProfileString("用户", "用户名", textBox_User_Name.Text, @"./Config.ini");
                    }
                    FileOpreate.WritePrivateProfileString("用户", "SU", "Y", @"./Config.ini");
                }
                else
                    checkBox_save_name.Checked = false;
            }
            else
            {
                textBox_User_Name.Text = "";
                FileOpreate.WritePrivateProfileString("用户", "用户名", "", @"./Config.ini");
                FileOpreate.WritePrivateProfileString("用户", "SU", "N", @"./Config.ini");
                checkBox_save_name.Checked = false;
            }
        }

        private void checkBox_save_pwd_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_save_name.Checked == true && checkBox_save_pwd.Checked == true)
            {
                if (textBox_User_Pwd.Text != "")
                {
                    if (textBox_User_Pwd.Text != FileOpreate.User_Pwd)
                    {
                        FileOpreate.User_Pwd = textBox_User_Pwd.Text;
                        FileOpreate.WritePrivateProfileString("用户", "密码", textBox_User_Pwd.Text, @"./Config.ini");
                    }
                    FileOpreate.WritePrivateProfileString("用户", "SP", "Y", @"./Config.ini");
                }
                else
                    checkBox_save_pwd.Checked = false;
            }
            else
            {
                textBox_User_Pwd.Text = "";
                FileOpreate.WritePrivateProfileString("用户", "密码", "", @"./Config.ini");
                FileOpreate.WritePrivateProfileString("用户", "SP", "N", @"./Config.ini");
                checkBox_save_pwd.Checked = false;
            }
        }

        private void button_login_Click(object sender, EventArgs e)
        {
            string username = textBox_User_Name.Text;
            string userpwd = textBox_User_Pwd.Text;
            if (username != "" && userpwd != "")
            {
                DataRow user_info = null;
                if (userinfodb.getUserInfoByName(username, out user_info) == true)
                {
                    if (user_info != null)
                    {
                        if (userpwd == user_info["PASSWORD"].ToString())
                        {
                            login_status = true;
                            this.Close();
                        }
                        else
                            MessageBox.Show("密码错误！");
                    }
                    else
                        MessageBox.Show("该用户不存在！");
                }
                else
                    MessageBox.Show("数据查询失败，请检查数据库连接！");
            }
            else
                MessageBox.Show("请输入用户名或密码后再点登录！");
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            login_status = false;
            this.Close();
        }

        private void LoginFrom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (panel_login.Visible == true)
                    button_login.PerformClick();
                if (panel_config.Visible == true)
                    button_SaveDbConfig.PerformClick();
            }
        }

        private void comboBox_Interface_Area_SelectedIndexChanged(object sender, EventArgs e)
        {
            FileOpreate.Interface_Area = comboBox_Interface_Area.SelectedIndex;
            if (comboBox_Interface_Area.SelectedIndex == 2 || comboBox_Interface_Area.SelectedIndex == 3)
            {
                panel_NM_zl_sq.Visible = true;
                textBox_NM_zlkzr.Text = FileOpreate.NM_Zlkzr;
                textBox_NM_sqr.Text = FileOpreate.NM_Sqr;
            }
            else
                panel_NM_zl_sq.Visible = false;
        }
                
        private void buttonSaveTestLineInfo_Click(object sender, EventArgs e)
        {
            try
            {
                FileOpreate.jcff.sds = checkBox14.Checked;
                FileOpreate.jcff.asm = checkBox15.Checked;
                FileOpreate.jcff.vmas = checkBox16.Checked;
                FileOpreate.jcff.jzjs = checkBox17.Checked;
                FileOpreate.jcff.zyjs = checkBox18.Checked;
                FileOpreate.WritePrivateProfileString("检测方法", "SDS", FileOpreate.jcff.sds ? "Y" : "N", @"./Config.ini");
                FileOpreate.WritePrivateProfileString("检测方法", "ASM", FileOpreate.jcff.asm ? "Y" : "N", @"./Config.ini");
                FileOpreate.WritePrivateProfileString("检测方法", "VMAS", FileOpreate.jcff.vmas ? "Y" : "N", @"./Config.ini");
                FileOpreate.WritePrivateProfileString("检测方法", "JZJS", FileOpreate.jcff.jzjs ? "Y" : "N", @"./Config.ini");
                FileOpreate.WritePrivateProfileString("检测方法", "ZYJS", FileOpreate.jcff.zyjs ? "Y" : "N", @"./Config.ini");
                
                MessageBox.Show("可检方法设置保存成功！", "保存成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show("原因：" + ex.Message, "可检方法保存失败");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            panel_login.Visible = true;
            panel_config.Visible = false;
        }
    }
}
