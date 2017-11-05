using System;
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
using System.IO;
using Microsoft.Win32;
using System.Data;
using Accord;
using Accord.MachineLearning;
using Accord.Statistics.Filters;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.Math.Optimization.Losses;
using Accord.MachineLearning.DecisionTrees.Rules;


namespace LAPAN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BarButtonItem_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            var file = string.Empty;
            if (openFileDialog.ShowDialog() == true)
            {
                string pathFile = openFileDialog.FileName;
                List<string[]> rows = File.ReadAllLines(pathFile).Select(x => x.Split(',')).ToList();
                List<TutupanLahan> lisdata = new List<TutupanLahan>(); 

                //add cols to datatable:
                string[] labelnya = rows[0];
                rows.RemoveAt(0);
                int no = 1;
            
                foreach (var item in rows)
                {
                    var data = new TutupanLahan();
                    data.no = Convert.ToString(no++);
                    data.band1 = item[0];
                    data.band2 = item[1];
                    data.band3 = item[2];
                    data.kelas = item[3];
                    lisdata.Add(data);
                }
              
                GridSource.DataContext = lisdata;
                RowsCount.Content = lisdata.Count;

                DecisinTree(labelnya, rows);
            }
        }

        public void DecisinTree(string[] labelnya, List<string[]> data)
        {


            // The first four columns contain the flower features

            string[][] text = data.ToArray<string[]>();
            
            // The first four columns contain the flower features
            double[][] inputs = new double[data.Count][];
            string[] targets = new string[data.Count];

            for (int i = 0; i < data.Count; ++i)
            {
                inputs[i] = text[i].Skip(0).Take(text[i].Length - 1).Select(double.Parse).ToArray();
                targets[i] = text[i][3];
            }

            string[] labels = targets;

            // Since the labels are represented as text, the first step is to convert
            // those text labels into integer class labels, so we can process them
            // more easily. For this, we will create a codebook to encode class labels:
            // 
            var codebook = new Codification("Output", labels);

            // With the codebook, we can convert the labels:
            int[] outputs = codebook.Translate("Output", labels);

            // And we can use the C4.5 for learning:
            C45Learning teacher = new C45Learning();

            // Finally induce the tree from the data:
            var tree = teacher.Learn(inputs, outputs);

            // To get the estimated class labels, we can use
            int[] predicted = tree.Decide(inputs);

            // The classification error (0.0266) can be computed as 
            double error = new ZeroOneLoss(outputs).Loss(predicted);

            // Moreover, we may decide to convert our tree to a set of rules:
            DecisionSet rules = tree.ToRules();

            // And using the codebook, we can inspect the tree reasoning:
            string ruleText = rules.ToString(codebook, "Output",
                System.Globalization.CultureInfo.InvariantCulture);
            List<string> rule = ruleText.Split(new[] { "\r\n" }, StringSplitOptions.None)
                             .ToList();
            List<string[]> rows = rule.Select(x => x.Split(':')).ToList();

            List<Rule> datarules = new List<Rule>();
            int no=1;
            foreach (var item in rows)
            {
                if (item.Count() >= 2)
                {
                    var r = new Rule();
                    r.no = no++;
                    r.kelas = item[0].Replace('=',' ');
                    r.rule = item[1].ToString();
                    datarules.Add(r);
                }
            }

            GridSourceRule.DataContext = datarules;
        }
    }
}
