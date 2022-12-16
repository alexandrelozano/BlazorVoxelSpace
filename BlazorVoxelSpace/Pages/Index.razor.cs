using Microsoft.AspNetCore.Components;
using System.Timers;
using Microsoft.JSInterop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using BlazorPro.BlazorSize;
using Aptacode.BlazorCanvas;

namespace BlazorVoxelSpace.Pages
{
    public class Camera
    {
        public float x;
        public float y;
        public float height;
        public float angle;
        public float horizon;
        public float distance;
    }

    public class Map
    {
        public int width;
        public int height;
        public int widthperiod;
        public int heightperiod; 
        public int shift;
        public int[] altitude;
        public int[] color;
    }

    public class Input
    {
        public int forwardbackward;
        public int leftright;
        public int updown;
        public bool lookup;
        public bool lookdown;
        public int mouseposition;
        public bool keypressed;
    }

    public partial class Index : ComponentBase
    {
        [Inject] 
        IJSRuntime JSRuntime { get; set; }

        [Inject]
        ResizeListener listener { get; set; }

        public BlazorCanvas Canvas { get; set; }

        BrowserWindowSize browserSize;
        static int maxScreenWidth = 640;
        static int screenBackGroundColor = RGBToint(200, 100, 100);
        
        int[] screen;
        int[] screenBG;

        int[] hiddeny;
        int[] hiddenyOrig;

        private System.Timers.Timer _timer;
        DateTime _lastTime; // marks the beginning the measurement began
        int _framesRendered; // an increasing count
        int _fps; // the FPS calculated from the last measurement
        List<KeyValuePair<string, string>> maps = new List<KeyValuePair<string, string>>();
        Camera camera;
        Map map;
        Input input;
        DateTime time;
        bool updaterunning;
        string info;
        string isDevice;
        bool mobile;
        string _divControlsDisplay;
        string _divInfoTextSize;

        protected string leftright()
        {
            if (camera!=null)
            {
                return camera.angle.ToString();
            }
            else
            {
                return "";
            }
        }

        protected static int RGBToint(byte r, byte g, byte b)
        { 
            return (255 << 24) | (r << 16) | (g << 8) | b;
        }

        public async Task FindResponsiveness()
        {
            mobile = await JSRuntime.InvokeAsync<bool>("isDevice");
            isDevice = mobile ? "Mobile" : "Desktop";
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await FindResponsiveness();

                if (!mobile)
                {
                    _divControlsDisplay = "display: none;";
                    _divInfoTextSize = "font-size: 120%;";
                }
                else
                {
                    _divInfoTextSize = "font-size: 60%;";
                }

                OnResizeWindow(null, await listener.GetBrowserWindowSize());
                listener.OnResized += OnResizeWindow;

                camera = new Camera() { x = 512, y = 800, height = 78, angle = 0, horizon = 100, distance = 800 };
                input = new Input() { forwardbackward = 0, leftright = 0, updown = 0, keypressed = false, lookdown = false, lookup = false, mouseposition = 0 };
                time = DateTime.Now;
                updaterunning = false;

                maps.Add(new KeyValuePair<string, string>("Map C1W","C1W;D1"));
                maps.Add(new KeyValuePair<string, string>("Map C2W","C2W;D2"));
                maps.Add(new KeyValuePair<string, string>("Map C3", "C3;D3"));
                maps.Add(new KeyValuePair<string, string>("Map C4", "C4;D4"));
                maps.Add(new KeyValuePair<string, string>("Map C5W", "C5W;D5"));
                maps.Add(new KeyValuePair<string, string>("Map C6W", "C6W;D6"));
                maps.Add(new KeyValuePair<string, string>("Map C7W", "C7W;D7"));
                maps.Add(new KeyValuePair<string, string>("Map C8", "C8;D6"));
                maps.Add(new KeyValuePair<string, string>("Map C9W", "C9W;D9"));
                maps.Add(new KeyValuePair<string, string>("Map C10W", "C10W;D10"));
                maps.Add(new KeyValuePair<string, string>("Map C11W", "C11W;D11"));
                maps.Add(new KeyValuePair<string, string>("Map C12W", "C12W;D11"));
                maps.Add(new KeyValuePair<string, string>("Map C13", "C13;D13"));
                maps.Add(new KeyValuePair<string, string>("Map C14", "C14W;D14"));
                maps.Add(new KeyValuePair<string, string>("Map C15", "C15;D15"));
                maps.Add(new KeyValuePair<string, string>("Map C16W", "C16W;D16"));
                maps.Add(new KeyValuePair<string, string>("Map C17W", "C17W;D17"));
                maps.Add(new KeyValuePair<string, string>("Map C18W", "C18W;D18"));
                maps.Add(new KeyValuePair<string, string>("Map C19W", "C19W;D19"));
                maps.Add(new KeyValuePair<string, string>("Map C20W", "C20W;D20"));
                maps.Add(new KeyValuePair<string, string>("Map C21", "C21;D21"));
                maps.Add(new KeyValuePair<string, string>("Map C22W", "C22W;D22"));
                maps.Add(new KeyValuePair<string, string>("Map C23W", "C23W;D21"));
                maps.Add(new KeyValuePair<string, string>("Map C24W", "C24W;D24"));
                maps.Add(new KeyValuePair<string, string>("Map C25W", "C25W;D25"));
                maps.Add(new KeyValuePair<string, string>("Map C26W", "C26W;D18"));
                maps.Add(new KeyValuePair<string, string>("Map C27W", "C27W;D15"));
                maps.Add(new KeyValuePair<string, string>("Map C28W", "C28W;D25"));
                maps.Add(new KeyValuePair<string, string>("Map C29W", "C29W;D16"));

                loadMap(maps[0].Value.ToString().Split(";")[0], maps[0].Value.ToString().Split(";")[1]);

                await JSRuntime.InvokeAsync<bool>("InitDotNetObject", DotNetObjectReference.Create(this));

                _timer = new System.Timers.Timer(1);
                _timer.Elapsed += NotifyTimerElapsed;
                _timer.Enabled = true;
            }
        }

        async void OnResizeWindow(object _, BrowserWindowSize window)
        {
            // Get the browsers's width / height
            browserSize = window;
            if (window.Width > maxScreenWidth) browserSize.Width = maxScreenWidth;
            
            float aspect = (float)((float)browserSize.Width / (float)browserSize.Height);
            browserSize.Height = (int)(browserSize.Width / aspect);

            screen = new int[browserSize.Width * browserSize.Height];
            screenBG = new int[browserSize.Width * browserSize.Height];

            Parallel.For(0, browserSize.Width * browserSize.Height, i => {
                screenBG[i] = screenBackGroundColor;
            });

            hiddeny = new int[browserSize.Width];
            hiddenyOrig = new int[browserSize.Width];
            Parallel.For(0, browserSize.Width-1, i => {
                hiddenyOrig[i] = browserSize.Height;
            });

            DrawBackground();

            // We're outside of the component's lifecycle, be sure to let it know it has to re-render.
            StateHasChanged();
        }

        void OnSelect(ChangeEventArgs e)
        {
            string[] s = e.Value.ToString().Split(";");
            loadMap(s[0], s[1]);
        }

        [JSInvokable("OnKeyDown")]
        public void OnKeyDown(int keyCode)
        {
            switch (keyCode)
            {
                case 37:    // left cursor
                case 65:    // a
                    input.leftright = +1;
                    break;
                case 39:    // right cursor
                case 68:    // d
                    input.leftright = -1;
                    break;
                case 38:    // cursor up
                case 87:    // w
                    input.forwardbackward = 3;
                    break;
                case 40:    // cursor down
                case 83:    // s
                    input.forwardbackward = -3;
                    break;
                case 82:    // r
                    input.updown = +2;
                    break;
                case 70:    // f
                    input.updown = -2;
                    break;
                case 69:    // e
                    input.lookup = true;
                    break;
                case 81:    //q
                    input.lookdown = true;
                    break;
                default:
                    break;
            }

            if (!updaterunning)
            {
                time = DateTime.Now;
                Draw();
            }

            StateHasChanged();
        }

        [JSInvokable("OnKeyUp")]
        public void OnKeyUp(int keyCode)
        {
            switch (keyCode)
            {
                case 37:    // left cursor
                case 65:    // a
                    input.leftright = 0;
                    break;
                case 39:    // right cursor
                case 68:    // d
                    input.leftright = 0;
                    break;
                case 38:    // cursor up
                case 87:    // w
                    input.forwardbackward = 0;
                    break;
                case 40:    // cursor down
                case 83:    // s
                    input.forwardbackward = 0;
                    break;
                case 82:    // r
                    input.updown = 0;
                    break;
                case 70:    // f
                    input.updown = 0;
                    break;
                case 69:    // e
                    input.lookup = false;
                    break;
                case 81:    //q
                    input.lookdown = false;
                    break;
                default:
                    break;
            }
        }

        private void NotifyTimerElapsed(Object source, ElapsedEventArgs e)
        {
            _framesRendered++;

            if ((DateTime.Now - _lastTime).TotalSeconds >= 1)
            {
                // one second has elapsed
                _fps = _framesRendered;
                _framesRendered = 0;
                _lastTime = DateTime.Now;
            }

            Draw();
        }

        async void loadMap(string textureMapFile, string heightMapFile)
        {
            Image<Rgba32> textureMap;
            Image<Rgba32> heightMap;

            map = new Map();
            map.width = 1024;
            map.height = 1024;
            map.widthperiod = map.width - 1;
            map.heightperiod = map.height - 1;
            map.shift = 10;
            map.color = new int[map.width * map.height];
            map.altitude = new int[map.width * map.height];
            textureMap = await loadImageFromURL("https://alexandrelozano.github.io/BlazorVoxelSpace/maps/" + textureMapFile + ".png");
            heightMap = await loadImageFromURL("https://alexandrelozano.github.io/BlazorVoxelSpace/maps/" + heightMapFile + ".png");

            fillMap(textureMap, heightMap);
        }

        void fillMap(Image<Rgba32> textureMap, Image<Rgba32> heightMap)
        {
            textureMap.TryGetSinglePixelSpan(out var pixelSpanTexture);
            heightMap.TryGetSinglePixelSpan(out var pixelSpanHeight);

            for (int i = 0; i < map.width * map.height; i++)
            {
                map.color[i] = (int)pixelSpanTexture[i].PackedValue;
                map.altitude[i] = pixelSpanHeight[i].R;
            }
        }

        async Task<Image<Rgba32>> loadImageFromURL(string url)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(url);
            Stream inputStream = await response.Content.ReadAsStreamAsync();
            Console.WriteLine("lenght:"+inputStream.Length);
            Image<Rgba32>img = Image.Load<Rgba32>(inputStream);
            img.Mutate(x => x.Resize((int)map.width, (int)map.height));

            return img;
        }

        void Draw()
        {
            updaterunning = true;

            UpdateCamera();
            DrawBackground();      
            Render();
            DrawCanvas();

            if (!input.keypressed)
            {
                updaterunning = false;
            }
            else
            {
                _timer.Enabled = true;
            }
        }

        void UpdateCamera()
        {
            var current = DateTime.Now;

            input.keypressed = false;
            if (input.leftright != 0)
            {
                camera.angle += (float)(input.leftright * 0.1 * (current - time).TotalMilliseconds * 0.03);
                if (camera.angle> 6.28318531f)
                {
                    camera.angle -= 6.28318531f;
                }
                else if (camera.angle < -6.28318531f)
                {
                    camera.angle += 6.28318531f;
                }

                input.keypressed = true;
            }
            if (input.forwardbackward != 0)
            {
                camera.x -= (float)(input.forwardbackward * Sin(camera.angle) * (current - time).TotalMilliseconds * 0.03);
                camera.y -= (float)(input.forwardbackward * Cos(camera.angle) * (current - time).TotalMilliseconds * 0.03);
                input.keypressed = true;
            }
            if (input.updown != 0)
            {
                camera.height += (float)(input.updown * (current - time).TotalMilliseconds * 0.03);
                input.keypressed = true;
            }
            if (input.lookup)
            {
                camera.horizon += (float)(2 * (current - time).TotalMilliseconds * 0.09);
                input.keypressed = true;
            }
            if (input.lookdown)
            {
                camera.horizon -= (float)(2 * (current - time).TotalMilliseconds * 0.09);
                input.keypressed = true;
            }

            // Collision detection. Don't fly below the surface.
            var mapoffset = (((uint)Math.Floor(camera.y) & (uint)(map.width - 1)) << (int)map.shift) + ((uint)Math.Floor(camera.x) & (map.height - 1));
            if ((map.altitude[mapoffset] + 10) > camera.height) camera.height = map.altitude[mapoffset] + 10;

            time = current;
        }

        void DrawBackground()
        {
            Array.Copy(screenBG, screen, screenBG.Length);
        }

        protected void DrawCanvas() {
            Canvas.SetImageBuffer(screen);
            Canvas.DrawImageBuffer(0, 0, browserSize.Width, browserSize.Height);
        }

        protected void Render()
        {
            if (map == null) return;

            var sinang = Sin(camera.angle);
            var cosang = Cos(camera.angle);

            Array.Copy(hiddenyOrig, hiddeny, hiddenyOrig.Length);

            float deltaz = (float)1.0;

            // Draw from front to back
            for (float z = 1; z < camera.distance; z += deltaz)
            {
                // 90 degree field of view
                var plx = -cosang * z - sinang * z;
                var ply = sinang * z - cosang * z;
                var prx = cosang * z - sinang * z;
                var pry = -sinang * z - cosang * z;

                var dx = (prx - plx) / browserSize.Width;
                var dy = (pry - ply) / browserSize.Width;
                plx += camera.x;
                ply += camera.y;
                float invz = (float)(1.0 / z * 240.0);

                for (int i = 0; i < browserSize.Width; i = i + 1)
                {
                    var mapoffset = (((int)(ply + 0.5d) & map.widthperiod) << (int)map.shift) + ((int)(plx + 0.5d) & map.heightperiod);
                    var heightonscreen = (int)((camera.height - map.altitude[mapoffset]) * invz + camera.horizon);
                    DrawVerticalLine(i, heightonscreen, hiddeny[i], map.color[mapoffset]);
                    if (heightonscreen < hiddeny[i]) hiddeny[i] = heightonscreen;
                    plx += dx;
                    ply += dy;
                }

                deltaz += (float)0.005;
            }

            void DrawVerticalLine(int x, int ytop, int ybottom, int col)
            {
                if (ytop < 0) ytop = 0;
                if (ytop > ybottom) return;

                // get offset on screen for the vertical line
                var offset = (ytop * browserSize.Width) + x;
                for (var k = ytop; k < ybottom ; k = k + 1)
                {
                    screen[offset] = col;
                    offset = offset + browserSize.Width;
                }
            }
        }

        public static float Sin(float x) //x in radians
        {
            float sinn;
            if (x < -3.14159265f)
                x += 6.28318531f;
            else
            if (x > 3.14159265f)
                x -= 6.28318531f;

            if (x < 0)
            {
                sinn = 1.27323954f * x + 0.405284735f * x * x;

                if (sinn < 0)
                    sinn = 0.225f * (sinn * -sinn - sinn) + sinn;
                else
                    sinn = 0.225f * (sinn * sinn - sinn) + sinn;
                return sinn;
            }
            else
            {
                sinn = 1.27323954f * x - 0.405284735f * x * x;

                if (sinn < 0)
                    sinn = 0.225f * (sinn * -sinn - sinn) + sinn;
                else
                    sinn = 0.225f * (sinn * sinn - sinn) + sinn;
                return sinn;

            }
        }

        public static float Cos(float x) //x in radians
        {
            return Sin(x + 1.5707963f);
        }
    }
}