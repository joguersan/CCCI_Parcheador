using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace Parcheador3DS
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            string plataforma;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    plataforma = "unix";
                    break;
                case PlatformID.Unix:
                    plataforma = "unix";
                    break;

                default:
                    plataforma = "windows";
                    break;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog archivoOriginal = new OpenFileDialog();
            archivoOriginal.Filter = "Dump de juego 3DS|*.cxi";
            archivoOriginal.FilterIndex = 1;

            archivoOriginal.Multiselect = false;
            if (archivoOriginal.ShowDialog() == DialogResult.OK)
            {
                string fullPath = Path.GetFullPath(archivoOriginal.FileName);
                textBox1.Text = fullPath;
            }
            else
            {
                MessageBox.Show("No se ha seleccionado ningún archivo. No podrás realizar el parcheo.", "Selecciona un archivo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Directory.Exists("temp"))
            {
                Directory.Delete("temp", true);
            }
        }
        private void Parchear_Click(object sender, EventArgs e)
        {
            string region = "";
            string ntrPatch = "";
            string xdt = "";
            if (textBox1.Text != "")
            {
                MessageBox.Show("Se va a realizar el proceso de parcheo. Espera hasta que termine y no cierres la aplicación.", "Comienza el proceso.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (usa.Checked||eur.Checked)
                {
                    if (usa.Checked)
                    {
                        region = "00040000001a6600";
                        ntrPatch = "ntrusa";
                        xdt = "parcheUSAD";
                    }
                    else if (eur.Checked)
                    {
                        region = "00040000001a6f00";
                        ntrPatch = "ntreur";
                        xdt = "parcheEURD";
                    }
                    if (Directory.Exists("temp"))
                    {
                        Directory.Delete("temp", true);
                    }
                    DirectoryInfo temporal = Directory.CreateDirectory("temp");
                    Directory.CreateDirectory("temp/original");
                    Directory.CreateDirectory("temp/modificado");
                    Directory.CreateDirectory("temp/extraido");
                    Directory.CreateDirectory("temp/parches");
                    temporal.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                    File.WriteAllBytes("temp/xdelta3.exe", Properties.Resources.xdelta3);
                    File.WriteAllBytes("temp/3dstools.exe", Properties.Resources._3dstools);
                    File.WriteAllBytes("temp/makerom.exe", Properties.Resources.makerom);
                    if (xdt == "parcheEURD")
                    {
                        File.WriteAllBytes("temp/parches/parcheEURD.xdelta", Properties.Resources.romfs);
                    }
                    else if (xdt == "parcheUSAD")
                    {
                        File.WriteAllBytes("temp/parches/parcheUSAD.xdelta", Properties.Resources.romfs);
                    }
                    label2.Text= "Paso 1/5. Extrayendo romfs.bin";
                    label2.Refresh();
                    ProcessStartInfo process = new ProcessStartInfo();
                    {
                        string program = "temp/3dstools.exe";
                        string arguments = "-xvtf cxi \"" + textBox1.Text + "\" --header \"temp/original/ncchheader.bin\" --exh \"temp/original/exheader.bin\" --exefs \"temp/original/exefs.bin\" --romfs \"temp/original/romfs.bin\" --logo \"temp/original/logo.bcma.lz\" --plain \"temp/original/plain.bin\"";
                        process.FileName = program;
                        process.Arguments = arguments;
                        process.UseShellExecute = false;
                        process.CreateNoWindow = true;
                        process.ErrorDialog = false;
                        process.RedirectStandardOutput = true;
                        Process x = Process.Start(process);
                        x.WaitForExit();
                    }
                    label2.Text = "Paso 2/5. Aplicando parche al romfs.bin";
                    label2.Refresh();
                    ProcessStartInfo xdelta = new ProcessStartInfo();
                    {
                        string program = "temp/xdelta3.exe";
                        string arguments = "";
                        if (xdt == "parcheEURD")
                        {
                            arguments = "-d -s \"" + Directory.GetCurrentDirectory() + "\\temp\\original\\romfs.bin\" \"" + Directory.GetCurrentDirectory() + "\\temp\\parches\\parcheEURD.xdelta\" \"" + Directory.GetCurrentDirectory() + "\\temp\\modificado\\romfs.bin\"";
                        }
                        else if (xdt == "parcheUSAD")
                        {
                            arguments = "-d -s \"" + Directory.GetCurrentDirectory() + "\\temp\\original\\romfs.bin\" \"" + Directory.GetCurrentDirectory() + "\\temp\\parches\\parcheUSAD.xdelta\" \"" + Directory.GetCurrentDirectory() + "\\temp\\modificado\\romfs.bin\"";
                        }
                        xdelta.FileName = program;
                        xdelta.Arguments = arguments;
                        xdelta.UseShellExecute = false;
                        xdelta.CreateNoWindow = true;
                        xdelta.ErrorDialog = true;
                        xdelta.RedirectStandardError = true;
                        xdelta.RedirectStandardOutput = true;
                        Process x2 = Process.Start(xdelta);
                        string error = x2.StandardError.ReadToEnd();
                        x2.WaitForExit();
                        if (error!="")
                        {
                            MessageBox.Show("No se ha podido parchear el juego. ¿Estás usando la versión adecuada?", "Error al parchear", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Directory.Delete("temp", true);
                            return;
                        }
                    }
                    if (luma.Checked || ntr.Checked)
                    {
                        File.WriteAllText("temp/lista.txt", Properties.Resources.lista);
                        string[] lista = File.ReadAllLines("temp/lista.txt");
                        if (!Directory.Exists("temp/extraido/romfs"))
                        {
                            Directory.CreateDirectory("temp/extraido/romfs");
                        }
                        label2.Text = "Paso 4/5. Extrayendo romfs.bin modificado";
                        label2.Refresh();
                        ProcessStartInfo extractROMFS = new ProcessStartInfo();
                        {
                            string program = "temp/3dstools.exe";
                            string arguments = "-xvtf romfs \"temp\\modificado\\romfs.bin\" --romfs-dir \"temp\\extraido\\romfs\"";
                            extractROMFS.FileName = program;
                            extractROMFS.Arguments = arguments;
                            extractROMFS.UseShellExecute = false;
                            extractROMFS.CreateNoWindow = true;
                            extractROMFS.ErrorDialog = false;
                            extractROMFS.RedirectStandardOutput = false;
                            Process xROMFS = Process.Start(extractROMFS);
                            xROMFS.WaitForExit();
                        }
                        if (ntr.Checked)
                        {
                            label2.Text = "Paso 5/5. Creando parche NTR.";
                            label2.Refresh();
                            for (int i = 0; i < lista.Length; i++)
                            {
                                string ruta = Path.GetDirectoryName(lista[i]);
                                if (!Directory.Exists("temp/final/plugin/" + region))
                                {
                                    Directory.CreateDirectory("temp/final/plugin/" + region);
                                }
                                if (!Directory.Exists("temp/final/LJT/CCCIDM/" + ruta))
                                {
                                    Directory.CreateDirectory("temp/final/LJT/CCCIDM/" + ruta);
                                }
                                File.Copy("temp/extraido/romfs/" + lista[i], "temp\\final\\LJT\\CCCIDM\\" + lista[i]);
                                if (ntrPatch == "ntreur")
                                {
                                    File.WriteAllText("temp/final/plugin/" + region + "/layeredfs.plg", Properties.Resources.ntreur);
                                }
                                else if (ntrPatch == "ntrusa")
                                {
                                        File.WriteAllText("temp/final/plugin/" + region + "/layeredfs.plg", Properties.Resources.ntreur);
                                }
                            }
                        }
                        else if (luma.Checked)
                        {
                            label2.Text = "Paso 5/5. Creando parche Luma.";
                            label2.Refresh();
                            for (int i = 0; i < lista.Length; i++)
                            {
                                string ruta = Path.GetDirectoryName(lista[i]);
                                if (!Directory.Exists("temp/final/luma/titles/"+region+"/romfs/" + ruta))
                                {
                                    Directory.CreateDirectory("temp/final/luma/titles/" + region + "/romfs/" + ruta);
                                }
                                File.Copy("temp/extraido/romfs/" + lista[i], "temp\\final\\luma\\titles\\" + region + "\\romfs\\" + lista[i]);
                            }
                        }
                    }
                    else if (DS.Checked || cia.Checked)
                    {
                        label2.Text = "Paso 4/5. Recreando archivo romfs.bin";
                        label2.Refresh();
                        if (!Directory.Exists("temp/final"))
                        {
                            Directory.CreateDirectory("temp/final");
                        }
                        ProcessStartInfo compCXI = new ProcessStartInfo();
                        {
                            string program = "temp/3dstools.exe";
                            string arguments = "-cvtf cxi \"temp\\modificado\\"+region+".cxi\" --header \"temp/original/ncchheader.bin\" --exh \"temp/original/exheader.bin\" --exefs \"temp/original/exefs.bin\" --romfs \"temp/modificado/romfs.bin\" --logo \"temp/original/logo.bcma.lz\" --plain \"temp/original/plain.bin\"";
                            compCXI.FileName = program;
                            compCXI.Arguments = arguments;
                            compCXI.UseShellExecute = false;
                            compCXI.CreateNoWindow = true;
                            compCXI.ErrorDialog = false;
                            compCXI.RedirectStandardOutput = true;
                            Process x3 = Process.Start(compCXI);
                            x3.WaitForExit();
                        }
                        if (DS.Checked)
                        {
                            label2.Text = "Paso 5/5. Creando archivo 3DS.";
                            label2.Refresh();
                            ProcessStartInfo comp3DS = new ProcessStartInfo();
                            {
                                string program = "\"temp/makerom.exe\"";
                                string arguments = "-f cci -o \"temp/final/CCCIESP.3ds\" -target t -i \"temp/modificado/"+region+".cxi\":0";
                                comp3DS.FileName = program;
                                comp3DS.Arguments = arguments;
                                comp3DS.UseShellExecute = false;
                                comp3DS.CreateNoWindow = true;
                                comp3DS.ErrorDialog = false;
                                comp3DS.RedirectStandardOutput = true;
                                Process x5 = Process.Start(comp3DS);
                                x5.WaitForExit();
                            }
                        }
                        else if (cia.Checked)
                        {
                            label2.Text = "Paso 5/5. Creando archivo CIA.";
                            label2.Refresh();
                            ProcessStartInfo compCIA = new ProcessStartInfo();
                            {
                                string program = "\"temp/makerom.exe\"";
                                string arguments = "-f cia -o temp/final/CCCIESP.cia -content temp/modificado/" + region + ".cxi:0:0";
                                compCIA.FileName = program;
                                compCIA.Arguments = arguments;
                                compCIA.UseShellExecute = false;
                                compCIA.CreateNoWindow = true;
                                compCIA.ErrorDialog = false;
                                compCIA.RedirectStandardOutput = true;
                                Process x4 = Process.Start(compCIA);
                                x4.WaitForExit();
                            }
                        }
                    }
                    label2.Text = "Proceso terminado.";
                    label2.Refresh();
                    if (luma.Checked||ntr.Checked)
                    {
                        var seleccion = MessageBox.Show("El parche se ha creado correctamente. ¿Quieres que lo apliquemos a tu juego?", "Proceso finalizado.", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        switch (seleccion)
                        {
                            case DialogResult.Yes:
                                Form2 frm = new Form2();
                                frm.Show();
                                break;
                            case DialogResult.No:    // No button pressed
                                string directorio = Directory.GetCurrentDirectory().ToString() + "/CCCI_LJT";
                                if (Directory.Exists(directorio))
                                {
                                    Directory.Delete(directorio, true);
                                }
                                Directory.Move("temp/final", directorio);
                                Process.Start(Directory.GetCurrentDirectory().ToString() + "/CCCI_LJT");
                                this.Close();
                                break;
                            default:                 // Neither Yes nor No pressed (just in case)
                                MessageBox.Show("¿Qué has pulsado?");
                                break;
                        }

                    }
                    else
                    {
                        string directorio = Directory.GetCurrentDirectory().ToString() + "/CCCI_LJT";
                        if (Directory.Exists(directorio))
                        {
                            Directory.Delete(directorio, true);
                        }
                        Directory.Move("temp/final", directorio);
                        Process.Start(directorio);
                        Directory.Delete("temp", true);
                    }
                }
            else
            {
                MessageBox.Show("Tienes que seleccionar la región de tu juego.", "Selecciona una región", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            MessageBox.Show("No se ha seleccionado ningún archivo, no podrás realizar el parcheo.", "Ningún archivo seleccionado", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }
    }
}
