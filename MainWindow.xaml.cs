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
using Accord.MachineLearning.DecisionTrees;


using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Geometry;

using OSGeo.GDAL;

namespace LAPAN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Rule> datarules = new List<Rule>();
        List<TutupanLahan> lisdata = new List<TutupanLahan>();
        DecisionTree tree = null;

        List<string[]> rowstest;
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
            tree = teacher.Learn(inputs, outputs);

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


            int no = 1;
            foreach (var item in rows)
            {
                if (item.Count() >= 2)
                {
                    var r = new Rule();
                    r.id = no++;
                    r.kelas = item[0].Replace('=', ' ');
                    r.rule = item[1].ToString();
                    datarules.Add(r);

                }
            }

            GridSourceRule.DataContext = datarules;
        }

        private void BarButtonItem_ItemClick_1(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            using (var db = new LapanContext())
            {
                try
                {
                    db.Rules.RemoveRange(db.Rules);
                    db.Rules.AddRange(datarules);
                    db.SaveChanges();
                    MessageBox.Show("Data Berhasil Tersimpan", "Keterangan");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }

        }

        private void BarButtonItem_ItemClick_2(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            var file = string.Empty;
            if (openFileDialog.ShowDialog() == true)
            {
                string pathFile = openFileDialog.FileName;
                List<string[]> rows = File.ReadAllLines(pathFile).Select(x => x.Split(',')).ToList();
                rowstest = rows;
                lisdata = new List<TutupanLahan>();
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


                GridSourceTest.DataContext = lisdata;

            }

        }

        private void BarButtonItem_ItemClick_3(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (rowstest == null)
            {
                MessageBox.Show("Mohon masukan Data Test");
                return;
            }
            string[][] text = rowstest.ToArray<string[]>();

            // The first four columns contain the flower features
            double[][] inputs = new double[rowstest.Count][];

            for (int i = 0; i < rowstest.Count; ++i)
            {
                inputs[i] = text[i].Skip(0).Take(text[i].Length - 1).Select(double.Parse).ToArray();
            }

            if (tree == null)
            {
                MessageBox.Show("Mohon Load Rule model terlebih dahulu");
                return;
            }
            int[] actual = tree.Decide(inputs);

            for (int i = 0; i < actual.Length; i++)
            {
                if (actual[i] == 0)
                    lisdata[i].kelas = "Air";
                else if (actual[i] == 1)
                    lisdata[i].kelas = "Tanah";
                else if (actual[i] == 2)
                    lisdata[i].kelas = "Vegetasi";
            }

            GridSourceTest.DataContext = lisdata;


        }

        private void RibbonControl_SelectedPageChanged(object sender, DevExpress.Xpf.Ribbon.RibbonPropertyChangedEventArgs e)
        {
            try
            {
                if (Ribbon.SelectedPage == Predict)
                {
                    PredictionGrid.Visibility = Visibility.Visible;
                    ModelingGrid.Visibility = Visibility.Collapsed;
                }
                else if (Ribbon.SelectedPage == Model)
                {
                    PredictionGrid.Visibility = Visibility.Collapsed;
                    ModelingGrid.Visibility = Visibility.Visible;
                }
                else if (Ribbon.SelectedPage == Map)
                {
                    PredictionGrid.Visibility = Visibility.Collapsed;
                    ModelingGrid.Visibility = Visibility.Collapsed;
                    MapGrid.Visibility = Visibility.Visible;
                    LoadMap();
                }
            }
            catch (Exception ae)
            {
                MessageBox.Show(ae.Message);
            }
        }

        async void LoadMap()
        {

            Map myMap = new Map(Basemap.CreateImagery());

            // Create uri to the map image layer
            //var serviceUri = new Uri(
            // "http://182.253.238.238:6080/arcgis/rest/services/Penutup_Lahan/MapServer");

            var serviceUri = new Uri(
               "http://182.253.238.238:6080/arcgis/rest/services/Penutup_Lahan_BW/MapServer");




            // Create new image layer from the url
            ArcGISMapImageLayer imageLayer = new ArcGISMapImageLayer(serviceUri);

            await imageLayer.LoadAsync();

            // Add created layer to the basemaps collection
            myMap.Basemap.BaseLayers.Add(imageLayer);


            // Assign the map to the MapView
            MyMapView.Map = myMap;

            await MyMapView.SetViewpointGeometryAsync(imageLayer.FullExtent);
        }

        private void BarButtonItem_ItemClick_4(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            using (var db = new LapanContext())
            {
                try
                {
                    datarules = db.Rules.ToList();
                    GridSourceRule2.DataContext = datarules;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }

        }

        private void BarButtonItem_ItemClick_5(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            var file = string.Empty;
            if (openFileDialog.ShowDialog() == true)
            {
                string pathFile = openFileDialog.FileName;
                try
                {
                    GdalConfiguration.ConfigureGdal();
                    Gdal.AllRegister();

                    Dataset ds = Gdal.Open(pathFile, Access.GA_ReadOnly);

                    if (ds == null)
                    {
                        Console.WriteLine("Can't open " + pathFile);
                        System.Environment.Exit(-1);
                    }

                    Console.WriteLine("Raster dataset parameters:");
                    Console.WriteLine("  Projection: " + ds.GetProjectionRef());
                    Console.WriteLine("  RasterCount: " + ds.RasterCount);
                    Console.WriteLine("  RasterSize (" + ds.RasterXSize + "," + ds.RasterYSize + ")");

                    Driver drv = ds.GetDriver();
                    if (drv == null)
                    {
                        Console.WriteLine("Can't get driver.");
                        System.Environment.Exit(-1);
                    }

                    Console.WriteLine("Using driver " + drv.LongName);

                    for (int iBand = 1; iBand <= ds.RasterCount; iBand++)
                    {
                        Band band = ds.GetRasterBand(iBand);
                        Console.WriteLine("Band " + iBand + " :");
                        Console.WriteLine("DataType: " + band.DataType);
                        Console.WriteLine("Size (" + band.XSize + "," + band.YSize + ")");
                        Console.WriteLine("PaletteInterp: " + band.GetRasterColorInterpretation().ToString());

                        for (int iOver = 0; iOver < band.GetOverviewCount(); iOver++)
                        {
                            Band over = band.GetOverview(iOver);
                            Console.WriteLine("OverView " + iOver + " :");
                            Console.WriteLine("DataType: " + over.DataType);
                            Console.WriteLine("Size (" + over.XSize + "," + over.YSize + ")");
                            Console.WriteLine("PaletteInterp: " + over.GetRasterColorInterpretation().ToString());
                        }
                    }

                }
                catch (Exception x)
                {
                    Console.WriteLine("Application error: " + x.Message);
                }

            }
        }

    }
}
