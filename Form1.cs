using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace ThreadDemo
{
    public partial class Form1 : Form
    {
        // danh sách quản lý 26 thread từ A-Z
        private MyThread[] threadLst;

        // kích thước 1 cell hiển thị
        private const int xCell = 30;
        private const int yCell = 30;

        // số cell theo chiều ngang và chiều dọc
        private const int xMax = 25;
        private const int yMax = 20;

        // đối tượng sinh số ngẫu nhiên
        private Random rnd = new Random();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // đối tượng quản lý tài nguyên của chương trình
            Assembly myAssembly = this.GetType().Assembly;

            // khởi tạo danh sách 26 thread từ A-Z
            threadLst = new MyThread[26];

            for (int i = 0; i < 26; i++)
            {
                threadLst[i] = new MyThread(rnd, xMax, yMax);

                // ký tự tương ứng từ A-Z
                char c = (char)(i + 65);

                // đọc bitmap miêu tả thread từ Resources
                using (Stream myStream = myAssembly.GetManifestResourceStream(
                    myAssembly.GetName().Name + ".Resources.Image" + c + ".bmp"))
                {
                    if (myStream != null)
                    {
                        threadLst[i].Pic = new Bitmap(myStream);
                    }
                }
            }

            // thiết lập kích thước vùng chạy
            ClientSize = new Size(xMax * xCell, yMax * yCell);

            // đặt Form ở góc trên trái màn hình
            this.Location = new Point(0, 0);

            // thiết lập màu nền
            this.BackColor = Color.Black;
        }

        // CÁC HÀM KHÁC SẼ DÁN VÀO ĐÂY
        // tạo mới và kích hoạt thread
        private void StartThread(MyThread currentThread, int keyCode)
        {
            // không tạo lại nếu thread đang chạy
            if (currentThread.IsRunning)
                return;

            currentThread.StopRequested = false;
            currentThread.IsRunning = true;
            currentThread.PauseEvent.Set();

            currentThread.Worker = new Thread(new ParameterizedThreadStart(Running));
            currentThread.Worker.IsBackground = true;

            // thread A ưu tiên cao nhất, các thread còn lại thấp nhất
            if (keyCode == 65)
                currentThread.Worker.Priority = ThreadPriority.Highest;
            else
                currentThread.Worker.Priority = ThreadPriority.Lowest;

            currentThread.Worker.Start(currentThread);
        }

        // tạm dừng thread
        private void PauseThread(MyThread currentThread)
        {
            if (!currentThread.IsRunning)
                return;

            currentThread.PauseEvent.Reset();
            currentThread.IsPaused = true;
        }

        // cho thread tiếp tục chạy
        private void ResumeThread(MyThread currentThread)
        {
            if (!currentThread.IsRunning)
                return;

            currentThread.PauseEvent.Set();
            currentThread.IsPaused = false;
        }

        // dừng thread
        private void StopThread(MyThread currentThread)
        {
            if (!currentThread.IsRunning)
                return;

            currentThread.StopRequested = true;
            currentThread.PauseEvent.Set();
        }
        // hàm mà mỗi thread sẽ thực hiện
        private void Running(object obj)
        {
            MyThread p = (MyThread)obj;

            try
            {
                // lặp cho đến khi có yêu cầu dừng
                while (!p.StopRequested)
                {
                    // nếu thread đang Pause thì chờ tại đây
                    p.PauseEvent.Wait();

                    // sau khi Resume, kiểm tra lại yêu cầu dừng
                    if (p.StopRequested)
                        break;

                    // ghi nhận vị trí hiện hành
                    Point oldPosition = p.Pos;

                    // cập nhật vị trí mới
                    p.HieuchinhVitri();

                    // yêu cầu Form vẽ lại icon
                    BeginInvoke(new Action(() =>
                    {
                        DrawThread(p, oldPosition);
                    }));

                    // giả lập thời gian thực hiện công việc
                    Thread.Sleep(100);
                }
            }
            finally
            {
                // ghi nhận thread đã kết thúc
                p.IsRunning = false;
                p.IsPaused = false;

                // xóa hình cuối cùng của thread
                BeginInvoke(new Action(() =>
                {
                    using (Graphics g = this.CreateGraphics())
                    {
                        using (Brush brush = new SolidBrush(this.BackColor))
                        {
                            g.FillRectangle(brush,
                                xCell * p.Pos.X,
                                yCell * p.Pos.Y,
                                xCell,
                                yCell);
                        }
                    }
                }));
            }
        }
        // hiển thị icon và hướng chạy của thread
        private void DrawThread(MyThread p, Point oldPosition)
        {
            using (Graphics g = this.CreateGraphics())
            {
                // xóa vị trí cũ
                using (Brush brush = new SolidBrush(this.BackColor))
                {
                    g.FillRectangle(brush,
                        xCell * oldPosition.X,
                        yCell * oldPosition.Y,
                        xCell,
                        yCell);
                }

                // hiển thị icon tại vị trí mới
                if (p.Pic != null)
                {
                    g.DrawImage(p.Pic, xCell * p.Pos.X, yCell * p.Pos.Y);

                    // xác định màu đối lập với icon
                    Color c = p.Pic.GetPixel(1, 1);
                    int yR = c.R > 128 ? 0 : 255;
                    int yG = c.G > 128 ? 0 : 255;
                    int yB = c.B > 128 ? 0 : 255;

                    using (Pen pen = new Pen(Color.FromArgb(yR, yG, yB), 2))
                    {
                        DrawDirectionArrow(g, pen, p);
                    }
                }
            }
        }

        // vẽ mũi tên biểu diễn hướng di chuyển
        private void DrawDirectionArrow(Graphics g, Pen pen, MyThread p)
        {
            int x;
            int y;
            int x1 = p.Pos.X;
            int y1 = p.Pos.Y;

            // góc dưới phải
            if (p.tx >= 0 && p.ty >= 0)
            {
                x = xCell * x1 + xCell - 2;
                y = yCell * y1 + yCell - 2;
                g.DrawLine(pen, x, y, x - 10, y);
                g.DrawLine(pen, x, y, x, y - 10);
            }
            // góc trên phải
            else if (p.tx >= 0 && p.ty < 0)
            {
                x = xCell * x1 + xCell - 2;
                y = yCell * y1 + 2;
                g.DrawLine(pen, x, y, x - 10, y);
                g.DrawLine(pen, x, y, x, y + 10);
            }
            // góc dưới trái
            else if (p.tx < 0 && p.ty >= 0)
            {
                x = xCell * x1 + 2;
                y = yCell * y1 + yCell - 2;
                g.DrawLine(pen, x, y, x + 10, y);
                g.DrawLine(pen, x, y, x, y - 10);
            }
            // góc trên trái
            else
            {
                x = xCell * x1 + 2;
                y = yCell * y1 + 2;
                g.DrawLine(pen, x, y, x + 10, y);
                g.DrawLine(pen, x, y, x, y + 10);
            }
        }

    // hàm xử lý gõ phím của user để quản lý thread
private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // xác định mã phím được nhấn
            int newch = e.KeyValue;

            // nếu không phải phím từ A-Z thì bỏ qua
            if (newch < 0x41 || newch > 0x5A)
                return;

            // xác định thread tương ứng với ký tự được nhấn
            int index = newch - 65;

            if (threadLst == null || threadLst[index] == null)
                return;

            MyThread currentThread = threadLst[index];

            // Ctrl + Shift + X: dừng thread X
            if (e.Control && e.Shift)
            {
                StopThread(currentThread);
            }
            // Ctrl + Alt + X: tạm dừng thread X
            else if (e.Control && e.Alt)
            {
                PauseThread(currentThread);
            }
            // Ctrl + X: giảm độ ưu tiên
            else if (e.Control)
            {
                if (currentThread.Worker != null && currentThread.Worker.IsAlive)
                {
                    currentThread.Worker.Priority = ThreadPriority.Lowest;
                    MessageBox.Show("Priority: " + currentThread.Worker.Priority.ToString());
                }
            }
            // Alt + X: chạy tiếp thread X
            else if (e.Alt)
            {
                ResumeThread(currentThread);
            }
            // Shift + X: tăng độ ưu tiên
            else if (e.Shift)
            {
                if (currentThread.Worker != null && currentThread.Worker.IsAlive)
                {
                    currentThread.Worker.Priority = ThreadPriority.Highest;
                    MessageBox.Show("Priority: " + currentThread.Worker.Priority.ToString());
                }
            }
            // X: tạo mới và kích hoạt thread X
            else
            {
                StartThread(currentThread, newch);
            }
        }

        // hàm xử lý khi đóng form
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (threadLst == null)
                return;

            // gửi yêu cầu dừng cho tất cả thread
            foreach (MyThread item in threadLst)
            {
                if (item == null)
                    continue;

                item.StopRequested = true;

                // đánh thức thread nếu đang Pause
                item.PauseEvent.Set();
            }

            // chờ các thread kết thúc
            foreach (MyThread item in threadLst)
            {
                if (item?.Worker != null && item.Worker.IsAlive)
                {
                    item.Worker.Join(500);
                }
            }
        }
    }
}

            