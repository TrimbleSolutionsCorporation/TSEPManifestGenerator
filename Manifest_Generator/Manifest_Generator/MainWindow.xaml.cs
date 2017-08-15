﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;

namespace Manifest_Generator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        private enum defaultHeaders
        {
            none            = 0,
            targetFolders   = 1,
            outputFolder    = 2,
            manifestName    = 3,
            manifestFolder  = 4,
            author          = 5
        }

        private string exePath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
        private string appName = "Manifest Generator";
        private string installerFolder = "%InstallerFolder%";

        string template =
        #region XMLTemplate
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<TEP Version=\"2.0\">" +
                "<Product Id=\"\" UpgradeCode=\"\" Version=\"1.0\" Language=\"1033\" Name=\"\" Manufacturer=\"\"  Description= \"\" IconPath=\"\"> " +
                    "<TeklaVersions>" +
                        "<TeklaVersion Name=\"\"/>" +
                        "<MinTeklaVersion Name=\"\"/>" +
                        "<MaxTeklaVersion Name=\"\"/>" +
                    "</TeklaVersions>" + 
                "</Product>" +
                "<SourcePathVariables>" + 
                    "<SourcePathVariable Id=\"TepOutputFolder\" Value=\"%TEPDEFINITIONFILEFOLDER%\\..\\output\" Comment=\"This provides the location where the package builder is to save the package.\"/>" +
                    "<SourcePathVariable Id=\"InstallerFolder\" Value=\"%TEPDEFINITIONFILEFOLDER%\"/>" +
                    "<SourcePathVariable Id=\"\" Value=\"\"/>" + 
                "</SourcePathVariables>" +
                "<TargetPathVariables>" +
                    "<PathVariable Id=\"\" Value=\"\"/>" + 
                "</TargetPathVariables>" +
                "<Component Id=\"TheExtensionComponent\" Guid=\"\">" +
                    "<File Id=\"\" Source=\"\" Target=\"\" Recursive =\"\" />" + 
                "</Component>" +
                "<Feature Id=\"TheExtensionFeature\" Title=\"ExtensionFeature\">" +
                    "<ComponentRef ReferenceId=\"TheExtensionComponent\" />" +
                "</Feature>" + 
            "</TEP>";
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            cmbSourceFolder.Text = exePath;
            txtSaveFolder.Text = exePath;
            readDefaultValues();
            cmbSourceFolder.Items.Add(installerFolder);
            cmbIcon.Items.Add(installerFolder);
            txtTSVersion.BorderBrush = Brushes.LightGreen;
            txtTSMINVersion.BorderBrush = Brushes.LightGreen;
            txtTSMAXVersion.BorderBrush = Brushes.LightGreen;
        }
        
        // Logic stuff
        private void readDefaultValues()
        {
            string path = exePath + @"\Defaults.ini";
            
            try
            {
                if (File.Exists(path))
                {                    
                    using (StreamReader reader = File.OpenText(path))
                    {
                        // get default values for dropdown menu and (some) text fields
                        string line;
                        defaultHeaders header = defaultHeaders.none;

                        while ((line = reader.ReadLine()) != null)
                        {
                            #region ParseFileAndHandleData
                            switch (line.Trim().ToLower())
                            {
                                case "":
                                    {
                                        continue;
                                    }
                                case "[target folders]":
                                    {
                                        header = defaultHeaders.targetFolders;
                                        break;
                                    }
                                case "[output folder]":
                                    {
                                        header = defaultHeaders.outputFolder;
                                        break;
                                    }
                                case "[manifest name]":
                                    {
                                        header = defaultHeaders.manifestName;
                                        break;
                                    }
                                case "[manifest folder]":
                                    {
                                        header = defaultHeaders.manifestFolder;
                                        break;
                                    }
                                case "[author]":
                                    {
                                        header = defaultHeaders.author;
                                        break;
                                    }
                                default:
                                    {
                                        switch (header)
                                        {
                                            case defaultHeaders.targetFolders:
                                                {
                                                    cmbTargetFolder.Items.Add(line);
                                                    break;
                                                }
                                            case defaultHeaders.outputFolder:
                                                {
                                                    txtOutputFolder.Text = line.Trim();
                                                    break;
                                                }
                                            case defaultHeaders.manifestName:
                                                {
                                                    txtManifestName.Text = line.Trim();
                                                    break;
                                                }
                                            case defaultHeaders.manifestFolder:
                                                {
                                                    txtSaveFolder.Text = line.Trim();
                                                    break;
                                                }
                                            case defaultHeaders.author:
                                                {
                                                    txtAuthor.Text = line.Trim();
                                                    break;
                                                }
                                            default:
                                                {
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                            }
                            #endregion
                        }
                    }
                }                                       
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Reading Defaults.ini failed!\n" + ex.Message,
                        "Manifest Generator", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }        
        }
        
        private Guid getGuid()
        {
            return Guid.NewGuid();
        }

        private string getSafeId(string text)
        {
            text = Regex.Replace(text, @"[^\w\s]", " "); // allow only word characters (a-z, 0-9, _) and white space, change others to space
            text = Regex.Replace(text, @"\s+", "_"); // convert all adjacent spaces to one underscore
            return text;
        }

        private string getVersion(string version)
        {
            // accept only values 2010.0 -> 2099.1
            if (Regex.IsMatch(version, @"^(20[1-9][0-9]\.[01])$"))
            {
                string[] versionNumbers = version.Split('.');

                int year = Convert.ToInt32(versionNumbers[0]);
                // accept only 2016.1 and newer versions
                if((year >= 2017) || ((year == 2016) && (versionNumbers[1][0] == '1')))
                {
                    return version;
                }
            }

            // accept only values 2010 -> 2099i
            if(Regex.IsMatch(version, @"^(20[1-9][0-9]i{0,1})$"))
            {
                if(version.Contains('i'))
                {
                    string[] versionNumbers = version.Split('i');

                    int year = Convert.ToInt32(versionNumbers[0]);
                    // accept only 2016i and newer versions
                    if (year >= 2016)
                    {
                        return versionNumbers[0] + ".1";
                    }
                }
                else
                {
                    int year = Convert.ToInt32(version);
                    // accept only 2017 and newer versions
                    if (year >= 2017)
                    {
                        return version + ".0";
                    }
                }
            }
            
            return null;
        }

        private bool handleProduct(XmlNode product)
        {
            product.Attributes[0].Value = getSafeId(txtName.Text);
            product.Attributes[1].Value = getGuid().ToString();
            // 2 == version, 3 == language: defaults are fine
            product.Attributes[4].Value = txtName.Text;
            product.Attributes[5].Value = txtAuthor.Text;
            product.Attributes[6].Value = txtDescription.Text;
            product.Attributes[7].Value = cmbIcon.Text; 
            XmlNode versions = product.ChildNodes[0];

            string version = getVersion(txtTSVersion.Text);
            string minVersion = getVersion(txtTSMINVersion.Text);
            string maxVersion = getVersion(txtTSMAXVersion.Text);

            if ((version != null) && (minVersion != null) && ((maxVersion != null) || txtTSMAXVersion.Text.Trim() == ""))
            {
                versions.ChildNodes[0].Attributes[0].Value = version;
                versions.ChildNodes[1].Attributes[0].Value = minVersion;
                
                // MAX version can be empty, remove the element if it is
                if (maxVersion != null)
                {
                    versions.ChildNodes[2].Attributes[0].Value = maxVersion;
                }
                else
                {
                    versions.RemoveChild(versions.ChildNodes[2]);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool handleFilesAndFolders(XmlNode source, XmlNode target, XmlNode component)
        {
            int id = 0;
            component.Attributes[1].Value = getGuid().ToString();

            string outputFolder = "%TEPDEFINITIONFILEFOLDER%\\" + txtOutputFolder.Text.Trim();
            source.ChildNodes[0].Attributes[1].Value = outputFolder;

            foreach(TreeViewItem item in treeView.Items)
            {
                if(id > 0)
                {
                    XmlNode targetNode = target.ChildNodes[0].Clone();
                    XmlNode sourceNode = source.ChildNodes[2].Clone();
                    XmlNode ComponentNode = component.ChildNodes[0].Clone();

                    target.AppendChild(targetNode);
                    source.AppendChild(sourceNode);
                    component.AppendChild(ComponentNode);
                }
                
                string targetPath = item.Header.ToString();                                
                string[] sourceString = ((TreeViewItem) item.Items[0]).Header.ToString().Split('|');
                string targetId = "TargetId" + id.ToString();
                string sourceId = "SourceId" + id.ToString();
                string fileId = "FileId" + id.ToString();
                string sourcePath = sourceString[0].Trim();
                string pattern = sourceString[1].Trim();
                string recursive = sourceString[2].Trim();

                if ((!pattern.Contains('?')) && (!pattern.Contains('*')))
                {
                    recursive = "no";
                }

                target.ChildNodes[id].Attributes[0].Value = targetId;
                target.ChildNodes[id].Attributes[1].Value = targetPath;
                source.ChildNodes[id + 2].Attributes[0].Value = sourceId;
                source.ChildNodes[id + 2].Attributes[1].Value = sourcePath;
                component.ChildNodes[id].Attributes[0].Value = fileId;
                component.ChildNodes[id].Attributes[1].Value = "%" + sourceId + "%\\" + pattern.Trim();
                component.ChildNodes[id].Attributes[2].Value = "%" + targetId + "%";
                component.ChildNodes[id].Attributes[3].Value = recursive;
                id++;
                
            }
            return true;
        }

        private XmlDocument generateXML()
        {
            XmlDocument manifest = new XmlDocument();
            manifest.LoadXml(template);
            
            XmlNode root = manifest.DocumentElement;

            if (handleProduct(root.ChildNodes[0]) && handleFilesAndFolders(root.ChildNodes[1], root.ChildNodes[2], root.ChildNodes[3]))
            {            
                return manifest;
            }
            else
            {
                return null;
            }
        }

        private TreeViewItem addFolderToTreeView(string folder, string folderShortName, TreeViewItem parent)
        {
            TreeViewItem treeItem = new TreeViewItem();
            bool isRecursive = true;

            treeItem.IsExpanded = true;

            // The first node (trunk) contains target folder information
            if (parent == null)
            {
                parent = new TreeViewItem();
                parent.IsExpanded = true;
                parent.Header = cmbTargetFolder.Text.Trim();
                isRecursive = (bool) chkRecursive.IsChecked;
                string recursive = (isRecursive) ? "yes" : "no";
                // The first branch contains full path and the pattern
                treeItem.Header = folder + " | " + txtPattern.Text + " | " + recursive;
            }
            // Subfolder branches contain only subfolder name and pattern, not full path
            else
            {
                treeItem.Header = folderShortName;
            }

            try
            {
                string[] files = getFiles(ref folder);

                if (files != null)
                {
                    // Add files from current folder to tree view
                    foreach (string file in files)
                    {
                        treeItem.Items.Add(new TreeViewItem() { Header = System.IO.Path.GetFileName(file) });
                    }

                    parent.Items.Add(treeItem);

                    if (isRecursive)
                    {
                        string[] subFolders = System.IO.Directory.GetDirectories(folder);

                        // Add subfolders recursively
                        foreach (string subFolder in subFolders)
                        {
                            addFolderToTreeView(subFolder, System.IO.Path.GetFileName(subFolder), treeItem);
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Adding file to file tree failed!\n" + ex.Message,
                        "Manifest Generator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return parent;
        }

        private string[] getFiles(ref string folder)
        {
            try
            {
                folder = replaceInstallerFolder(folder, txtSaveFolder.Text.Trim());
                string[] files = System.IO.Directory.GetFiles(folder, txtPattern.Text);
                return files;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Adding file to file tree failed!\n" + ex.Message,
                        "Manifest Generator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private bool saveFile(XmlDocument document, string manifestName, string saveLocation)
        {
            try
            {
                if (File.Exists(saveLocation + manifestName))
                {
                    System.Windows.MessageBoxResult dialogResult =
                        System.Windows.MessageBox.Show("Manifest file exists, do you want to overwrite?",
                        appName, MessageBoxButton.YesNo, MessageBoxImage.Question);

                    // exit function if file overwrite is not wanted
                    if (dialogResult != System.Windows.MessageBoxResult.Yes)
                    {
                        System.Windows.MessageBox.Show("Manifest file was not created",
                            appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }

                document.Save(saveLocation + manifestName);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Manifest file saving failed!\n" + ex.Message,
                    "Manifest Generator", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        private bool createFolder(string saveLocation)
        {
            System.Windows.MessageBoxResult dialogResult =
                System.Windows.MessageBox.Show("Save folder doesn't exist. Do you want to create the folder?",
                appName, MessageBoxButton.YesNo, MessageBoxImage.Question);

            // exit function if file overwrite is not wanted
            if (dialogResult == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    DirectoryInfo result = Directory.CreateDirectory(saveLocation);
                    if (result.Exists)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("Save folder creation failed!\n" + ex.Message,
                        "Manifest Generator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return false;
        }

        private string replaceInstallerFolder(string folder, string saveFolder)
        {
            return folder.Replace(installerFolder, saveFolder);
        }

        private string removeCommonPart(string path, string saveFolder)
        {
            if (path == "")
            {
                return "";
            }

            path = System.IO.Path.GetFullPath(replaceInstallerFolder(path, saveFolder));

            if (path.Contains(saveFolder))
            {
                // can be replaced with shortcut
                return path.Replace(saveFolder, installerFolder);
            }
            else if (System.IO.Path.GetPathRoot(path) == System.IO.Path.GetPathRoot(saveFolder))
            {
                // can be partially replaced with shortcut
                string combined = installerFolder;
                string[] pathParts = path.Split('\\');
                string[] saveFolderParts = saveFolder.Split('\\');

                int i = 0;

                while ((i < saveFolderParts.Length) && (i < pathParts.Length) && (pathParts[i] == saveFolderParts[i]))
                {
                    i++;
                }

                int j = i;
                while (i < saveFolderParts.Length)
                {
                    combined += "\\..";
                    i++;
                }

                while (j < pathParts.Length)
                {
                    combined += "\\" + pathParts[j++];
                }

                return combined;
            }
            else
            {
                // can't be replaced with shortcut
                return path;
            }
        }

        // UI stuff
        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            XmlDocument document = generateXML();
            string manifestName = getSafeId(txtManifestName.Text.Trim()) + ".xml";
            string saveLocation = txtSaveFolder.Text + "\\";
            bool result = false;

            if(document != null)
            {
                if (Directory.Exists(saveLocation))
                {
                    result = saveFile(document, manifestName, saveLocation);
                }
                else
                {
                    if (createFolder(saveLocation))
                    {
                        result = saveFile(document, manifestName, saveLocation);
                    }
                }                

                if (result)
                {
                    System.Windows.MessageBox.Show(string.Format("Manifest file {0} created in {1}", manifestName, saveLocation), 
                        appName, MessageBoxButton.OK, MessageBoxImage.None);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Manifest file was not created, check the version numbers",
                    appName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAddFile_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem tree = addFolderToTreeView(cmbSourceFolder.Text, System.IO.Path.GetFileName(cmbSourceFolder.Text.Trim()), null);
            if (tree != null)
            {
                treeView.Items.Add(tree);
            }
        }

        private void btnSelectSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (cmbSourceFolder.Text != "")
            {
                dialog.SelectedPath = System.IO.Path.GetFullPath(replaceInstallerFolder(cmbSourceFolder.Text, txtSaveFolder.Text));
            }

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                cmbSourceFolder.Text = dialog.SelectedPath;
                cmbSourceFolder.Text = removeCommonPart(cmbSourceFolder.Text, txtSaveFolder.Text);
            }
        }

        private void btnSelectSaveFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = txtSaveFolder.Text; 
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtSaveFolder.Text = dialog.SelectedPath;
            }
        }        

        private void btnSelectIconFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();

            dialog.InitialDirectory = replaceInstallerFolder(cmbIcon.Text, txtSaveFolder.Text);

            dialog.Filter = "image files (*.png)|*.png|All files (*.*)|*.*";
            dialog.FilterIndex = 1;
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                cmbIcon.Text = dialog.FileName;
                cmbIcon.Text = removeCommonPart(cmbIcon.Text, txtSaveFolder.Text);
            }
        }

        private void btnRemoveSelectedFiles_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)treeView.SelectedItem;
            
            treeView.Items.Remove(item);            
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            treeView.Items.Clear();
        }

        private void txtTSVersion_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (getVersion(txtTSVersion.Text) != null)
            {
                txtTSVersion.BorderBrush = Brushes.LightGreen;
                txtTSVersion.Background = Brushes.White;
            }
            else
            {
                txtTSVersion.BorderBrush = Brushes.Red;
                txtTSVersion.Background = Brushes.Pink;
            }
        }

        private void txtTSMINVersion_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (getVersion(txtTSMINVersion.Text) != null)
            {
                txtTSMINVersion.BorderBrush = Brushes.LightGreen;
                txtTSMINVersion.Background = Brushes.White;
            }
            else
            {
                txtTSMINVersion.BorderBrush = Brushes.Red;
                txtTSMINVersion.Background = Brushes.Pink;
            }
        }

        private void txtTSMAXVersion_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtTSMAXVersion.Text.Trim() == "" || getVersion(txtTSMAXVersion.Text) != null)
            {
                txtTSMAXVersion.BorderBrush = Brushes.LightGreen;
                txtTSMAXVersion.Background = Brushes.White;
            }
            else
            {
                txtTSMAXVersion.BorderBrush = Brushes.Red;
                txtTSMAXVersion.Background = Brushes.Pink;
            }
        }

        private void txtSaveFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((cmbIcon.Text != "") && (cmbIcon.Text != ""))
            {
                if (!Directory.Exists(System.IO.Path.GetFullPath(replaceInstallerFolder(cmbIcon.Text, txtSaveFolder.Text))))
                {
                    cmbIcon.Text = "";
                }

                if (!Directory.Exists(System.IO.Path.GetFullPath(replaceInstallerFolder(cmbSourceFolder.Text, txtSaveFolder.Text))))
                {
                    cmbSourceFolder.Text = "";
                }
            }
        }
    }
}