using System;
using System.Windows.Forms;
using Optimizer;
using System.Collections.Generic;
using Microsoft.Office.Interop;
using Microsoft.Office.Interop.Word;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing.Printing;
using System.Drawing;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CloseSolution();

            TableA.RowCount = 1; TableA.ColumnCount = 1;
            Table.RowCount = 1;
            Price.RowCount = 1;
            TableB.ColumnCount = 1; TableB.RowCount = 1;
            TableBforPrice.ColumnCount = 1; TableBforPrice.RowCount = 1;
            Table.ColumnCount = 1;
            Price.ColumnCount = 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            potencialBox.Items.Clear();
            evaluationBox.Items.Clear();

            #region Переменные
            int iteration = 1; // Номер итерации

            double min;

            int iMin;
            int jMin;

            int  // Кол-во отправителей и потребителей 
                n = int.Parse(nA.Text),
                m = int.Parse(mB.Text);

            double sumA = 0;
            for (int i = 0; i < n; i++)
                sumA += Convert.ToDouble(TableA.Rows[i].Cells[0].Value);

            double sumB = 0;
            for (int j = 0; j < m; j++)
                sumB += Convert.ToDouble(TableB.Rows[0].Cells[j].Value);

            if (sumA > sumB)
            {
                m++;
                TableB.ColumnCount++;
                Price.ColumnCount++;
                Table.ColumnCount++;
                TableBforPrice.ColumnCount++;
                mB.Text = (int.Parse(mB.Text) + 1).ToString();

                TableB.Rows[0].Cells[m - 1].Value = sumA - sumB;
                TableB.Rows[0].Cells[m - 1].Style.BackColor = TableBforPrice.Rows[0].Cells[m - 1].Style.BackColor = System.Drawing.Color.Plum;

                for (int i = 0; i < n; i++)
                    Price.Rows[i].Cells[Price.ColumnCount - 1].Value = 0;
            }
            else if (sumA < sumB)
            {
                n++;
                TableA.RowCount++;
                Price.RowCount++;
                Table.RowCount++;
                nA.Text = (int.Parse(nA.Text) + 1).ToString();

                TableA.Rows[n - 1].Cells[0].Value = sumB - sumA;
                TableA.Rows[n - 1].Cells[0].Style.BackColor = System.Drawing.Color.Plum;

                for (int j = 0; j < m; j++)
                    Price.Rows[Price.RowCount - 1].Cells[j].Value = 0;
            }


            var c = new double[n, m];  // Матрица стоимостей за единицу перевозок из DataGrid
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    c[i, j] = Convert.ToDouble(Price.Rows[i].Cells[j].Value);

            var A = new double[n]; // Запасы в пунктах отправления
            for (int i = 0; i < n; i++)
                A[i] = Convert.ToDouble(TableA.Rows[i].Cells[0].Value);

            var B = new double[m]; // Потребности пунктов потребления
            for (int j = 0; j < m; j++)
                B[j] = Convert.ToDouble(TableB.Rows[0].Cells[j].Value);
            #endregion

            var x = Transportation.GetFirstScheme(A, B); // Матрица распределений ресурсов с построенным первоначальным планом

            while (true)
            {
                var p = Transportation.GetPotentials(x, c); // Подсчет потенциалов
                var xTemporary = x;

                while (p == null)
                {
                    Random r = new Random();

                    int i = r.Next(n);
                    int j = r.Next(m);

                    while (xTemporary[i, j] != null)
                    {
                        i = r.Next(n);
                        j = r.Next(m);
                    }

                    xTemporary[i, j] = 0;

                    p = Transportation.GetPotentials(xTemporary, c);

                    if (p == null)
                        xTemporary = x;
                    else
                        x = xTemporary;
                }

                var delta = Transportation.Evaluation(x, c, p); // Подсчет оценок оптимальности


                potencialBox.Items.Add(string.Format("{0} ИТЕРАЦИЯ", iteration)); // Вывод потенциалов

                for (int i = 0; i < n; i++)
                    potencialBox.Items.Add(string.Format("U[{0}] = {1}", i + 1, p[0][i]));

                for (int j = 0; j < m; j++)
                    potencialBox.Items.Add(string.Format("V[{0}] = {1}", j + 1, p[1][j]));

                //for (int i = 0; i < n; i++)
                //{
                //    string s = "";

                //    for (int j = 0; j < m; j++)
                //    {
                //        if (x[i, j] == null)
                //            string.Format("{0, 4}|", "-");
                //        else s += string.Format("{0, 4}|", x[i, j].ToString());
                //    }

                //    potencialBox.Items.Add(s);
                //}

                evaluationBox.Items.Add(string.Format("{0} ИТЕРАЦИЯ", iteration)); //Вывод оценок оптимальности
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                        if (delta[i, j] != null)
                            evaluationBox.Items.Add(string.Format("D[{0},{1}] = {2}", i + 1, j + 1, delta[i, j]));

                min = Convert.ToInt32(delta[0, 0]);

                iMin = 0; jMin = 0; //Координаты ячейки, где находится наименьшая оценка

                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                        if (delta[i, j] < min)
                        {
                            min = Convert.ToInt32(delta[i, j]);
                            iMin = i;
                            jMin = j;
                        }

                if (min >= 0)
                {
                    cost.Text = Transportation.CalculateCost(x, c).ToString();

                    for (int i = 0; i < n; i++)
                        for (int j = 0; j < m; j++)
                            Table.Rows[i].Cells[j].Value = Convert.ToString(x[i, j]);

                    return;
                }

                var l = Transportation.GetLambda(x, iMin, jMin);

                var minL = Transportation.MinOfLambda(l, x);

                for (int i = 0; i < n; i++) //Перемещение l по циклу
                    for (int j = 0; j < m; j++)
                        if (l[i, j] == "+")
                        {
                            if (x[i, j] == null)
                                x[i, j] = 0;

                            x[i, j] += minL;
                        }
                        else if (l[i, j] == "-")
                        {
                            x[i, j] -= minL;

                            if (x[i, j] == 0)
                                x[i, j] = null;


                        }

                iteration++;
            }

        }

        private void nA_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (nA.Text == "" || int.Parse(nA.Text) > 100) return;
                TableA.RowCount = int.Parse(nA.Text);
                Table.RowCount = int.Parse(nA.Text);
                Price.RowCount = int.Parse(nA.Text);
            }
            catch
            {
                MessageBox.Show("Введено недопустимое значение (значение должно быть в границах от 0 до 80). Повторите ввод", "Ошибка ввода");
                return;
            }

        }

        private void mB_TextChanged(object sender, EventArgs e)
        {

            try
            {
                if (mB.Text == "" || int.Parse(mB.Text) > 100) return;
                TableB.ColumnCount = int.Parse(mB.Text);
                TableBforPrice.ColumnCount = int.Parse(mB.Text);
                Table.ColumnCount = int.Parse(mB.Text);
                Price.ColumnCount = int.Parse(mB.Text);
            }
            catch
            {
                MessageBox.Show("Введено недопустимое значение (значение должно быть в границах от 0 до 100). Повторите ввод", "Ошибка ввода");
                return;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (ShowSolution.Checked)
                OpenSolution();
            else
                CloseSolution();
        }

        void OpenSolution()
        {
            #region Видимость
            groupBox3.Visible = true;
            label2.Visible = true;
            label5.Visible = true;
            potencialBox.Visible = true;
            evaluationBox.Visible = true;
            #endregion

            this.Width = 830;
        }

        void CloseSolution()
        {
            #region Видимость
            groupBox3.Visible = false;
            label2.Visible = false;
            label5.Visible = false;
            potencialBox.Visible = false;
            evaluationBox.Visible = false;
            #endregion

            this.Width = 660;
        }

        //Вносит ПП в поле с матрицей стоимостей
        private void TableB_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            for (int i = 0; i < TableB.ColumnCount; i++)
                TableBforPrice.Rows[0].Cells[i].Value = TableB.Rows[0].Cells[i].Value;
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();

            about.labelProductName.Text = "Транспортная задача";
            //about.labelVersion.Text += "1.0 Beta";
            about.labelCopyright.Text = "Гавришин Евгений Юрьевич";
            about.labelCompanyName.Text = "ГПОУ ТО \"Донской колледж информационных технологий\" 2017";
            about.textBoxDescription.Text = "Данное приложение позволяет минимизировать затраты на перевозку однородного груза с множетсва складов на множество магазинов устанавливая оптимальный план. Метод решения, использованный при разработке данного приложения известен в математике как \"Метод потенциалов\"";

            about.Show();
        }

        private void экспортироватьВWordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var x = new string[Table.RowCount, Table.ColumnCount];
            var c = new string[Price.RowCount, Price.ColumnCount];

            for (int i = 0; i < x.GetLength(0); i++)
                for (int j = 0; j < x.GetLength(1); j++)
                {
                    x[i, j] = Table.Rows[i].Cells[j].Value.ToString();
                    c[i, j] = Price.Rows[i].Cells[j].Value.ToString();
                }

            var a = new string[TableA.RowCount];

            for (int i = 0; i < a.Length; i++)
                a[i] = TableA.Rows[i].Cells[0].Value.ToString();

            var b = new string[TableB.ColumnCount];

            for (int j = 0; j < b.Length; j++)
                b[j] = TableB.Rows[0].Cells[j].Value.ToString();

            var z = cost.Text;

            #region Создание документа
            var word = new Microsoft.Office.Interop.Word.Application();

            word.Visible = true; //Открывает Word

            var doc = word.Documents.Add();

            // Границы ячеек
            var param1 = WdDefaultTableBehavior.wdWord9TableBehavior;

            // Автоподгонка ячеек
            var param2 = WdAutoFitBehavior.wdAutoFitContent;

            word.Selection.TypeText("Результаты решения транспортной задачи:");

            int n = x.GetLength(0) + 1;
            int m = x.GetLength(1) + 1;

            word.ActiveDocument.Tables.Add(word.Selection.Range, n, m, param1, param2);

            //Так как ячейки Word нумеруются с 1 - циклы начинаются не с 0
            for (int i = 1; i < n; i++)
                word.ActiveDocument.Tables[1].Cell(i + 1, 1).Range.InsertAfter(a[i - 1]);

            for (int j = 1; j < m; j++)
                word.ActiveDocument.Tables[1].Cell(1, j + 1).Range.InsertAfter(b[j - 1]);


            for (int i = 2; i <= n; i++)
                for (int j = 2; j <= m; j++)
                    if (x[i - 2, j - 2] != "")
                        word.ActiveDocument.Tables[1].Cell(i, j).Range.InsertAfter(string.Format("{0} ({1})", x[i - 2, j - 2], c[i - 2, j - 2]));

            #endregion

            string pathToFile = Path.Combine(Environment.CurrentDirectory, "Solution.doc");

            if (File.Exists(pathToFile)) File.Delete(pathToFile);

            word.ActiveDocument.SaveAs2(pathToFile);
        }

        [Serializable]
        struct example
        {
            public int n, m;
            public double[] A;
            public double[] B;
            public double[,] c;
        }

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                example ex;

                ex.n = int.Parse(nA.Text);
                ex.m = int.Parse(mB.Text);

                ex.A = new double[ex.n];
                for (int i = 0; i < ex.n; i++)
                    ex.A[i] = Convert.ToDouble(TableA.Rows[i].Cells[0].Value);

                ex.B = new double[ex.m];
                for (int j = 0; j < ex.m; j++)
                    ex.B[j] = Convert.ToDouble(TableB.Rows[0].Cells[j].Value);

                ex.c = new double[ex.n, ex.m];
                for (int i = 0; i < ex.n; i++)
                    for (int j = 0; j < ex.m; j++)
                        ex.c[i, j] = Convert.ToDouble(Price.Rows[i].Cells[j].Value);

                savior.FileName = "Example.trp";
                savior.Filter = "Файл условий задачи *.trp|*.trp";

                if (savior.ShowDialog() == DialogResult.OK)
                {
                    FileStream fout = File.Create(savior.FileName);

                    BinaryFormatter bf = new BinaryFormatter();

                    bf.Serialize(fout, ex);

                    fout.Close();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Поля пусты");
            }
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            example ex;
            opener.Filter = "Файл условий задачи *.trp|*.trp";

            if (opener.ShowDialog() == DialogResult.OK)
            {
                FileStream fin = File.OpenRead(opener.FileName);

                BinaryFormatter bf = new BinaryFormatter();

                ex = (example)bf.Deserialize(fin);

                fin.Close();


                nA.Text = ex.n.ToString();
                mB.Text = ex.m.ToString();

                for (int i = 0; i < ex.n; i++)
                    TableA.Rows[i].Cells[0].Value = ex.A[i];

                for (int j = 0; j < ex.m; j++)
                    TableB.Rows[0].Cells[j].Value = ex.B[j];

                for (int i = 0; i < ex.n; i++)
                    for (int j = 0; j < ex.m; j++)
                        Price.Rows[i].Cells[j].Value = ex.c[i, j];
            }
        }


        Bitmap bmp;

        private void печатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Graphics graphics = this.CreateGraphics();

            bmp = new Bitmap(this.Size.Width, this.Size.Height, graphics);

            Graphics mg = Graphics.FromImage(bmp);

            mg.CopyFromScreen(this.Location.X, this.Location.Y, 0, 0, this.Size);

            printPreviewDialog.ShowDialog();
        }

        private void printDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            e.Graphics.DrawImage(bmp, 0, 0);
        }
    }
}
