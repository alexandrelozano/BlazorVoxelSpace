using Microsoft.AspNetCore.Components;
using System.Timers;
using System.Runtime.InteropServices;
using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using System.Net.Http;
using System.IO;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using BlazorPro.BlazorSize;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorVoxelSpace.Pages
{
    public class Camera
    {
        public double x;
        public double y;
        public double height;
        public double angle;
        public double horizon;
        public double distance;
    }

    public class Map
    {
        public uint width;
        public uint height;
        public uint shift;
        public uint[] altitude;
        public uint[] color;
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

        BrowserWindowSize browserSize;
        static int maxScreenWidth = 320;
        static uint screenBackGroundColor = 0xFFE09090;
        uint[] screen;
        private Timer _timer;
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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
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

                await JSRuntime.InvokeAsync<bool>("InitCanvas",browserSize.Width, browserSize.Height);
                await JSRuntime.InvokeAsync<bool>("InitDotNetObject", DotNetObjectReference.Create(this));

                _timer = new Timer(1);
                _timer.Elapsed += NotifyTimerElapsed;
                _timer.Enabled = true;
            }
        }

        async void OnResizeWindow(object _, BrowserWindowSize window)
        {
            // Get the browsers's width / height
            browserSize = window;
            if (window.Width > maxScreenWidth) browserSize.Width = maxScreenWidth;
            
            double aspect = (double)browserSize.Width / (double)browserSize.Height;
            browserSize.Height = (int)(browserSize.Width / aspect);

            screen = new uint[browserSize.Width * browserSize.Height];

            // We're outside of the component's lifecycle, be sure to let it know it has to re-render.
            StateHasChanged();

            await JSRuntime.InvokeAsync<bool>("InitCanvas", browserSize.Width, browserSize.Height);
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
            map.shift = 10;
            map.color = new uint[map.width * map.height];
            map.altitude = new uint[map.width * map.height];

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
                map.color[i] = pixelSpanTexture[i].PackedValue;
                map.altitude[i] = pixelSpanHeight[i].R;
            }
        }

        async Task<Image<Rgba32>> loadImageFromURL(string url)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(url);
            Stream inputStream = await response.Content.ReadAsStreamAsync();

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
            Flip();

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
                camera.angle += input.leftright * 0.1 * (current - time).TotalMilliseconds * 0.03;
                input.keypressed = true;
            }
            if (input.forwardbackward != 0)
            {
                camera.x -= input.forwardbackward * Math.Sin(camera.angle) * (current - time).TotalMilliseconds * 0.03;
                camera.y -= input.forwardbackward * Math.Cos(camera.angle) * (current - time).TotalMilliseconds * 0.03;
                input.keypressed = true;
            }
            if (input.updown != 0)
            {
                camera.height += input.updown * (current - time).TotalMilliseconds * 0.03;
                input.keypressed = true;
            }
            if (input.lookup)
            {
                camera.horizon += 2 * (current - time).TotalMilliseconds * 0.09;
                input.keypressed = true;
            }
            if (input.lookdown)
            {
                camera.horizon -= 2 * (current - time).TotalMilliseconds * 0.09;
                input.keypressed = true;
            }

            // Collision detection. Don't fly below the surface.
            var mapoffset = (((uint)Math.Floor(camera.y) & (uint)(map.width - 1)) << (int)map.shift) + ((uint)Math.Floor(camera.x) & (map.height - 1));
            if ((map.altitude[mapoffset] + 10) > camera.height) camera.height = map.altitude[mapoffset] + 10;

            time = current;
        }

        void DrawBackground()
        {
            for (uint i = 0; i < browserSize.Width * browserSize.Height; i++)
            {
                screen[i] = screenBackGroundColor;
            }
        }

        protected void Flip()
        {
            var gch = GCHandle.Alloc(screen, GCHandleType.Pinned);
            var pinned = gch.AddrOfPinnedObject();
            var mono = JSRuntime as WebAssemblyJSRuntime;
            mono.InvokeUnmarshalled<IntPtr, string>("PaintCanvas", pinned);
            gch.Free();
        }

        protected void Render()
        {
            if (map == null) return;

            var mapwidthperiod = map.width - 1;
            var mapheightperiod = map.height - 1;

            var sinang = Math.Sin(camera.angle);
            var cosang = Math.Cos(camera.angle);

            uint[] hiddeny = new uint[browserSize.Width];
            for (int i = 0; i < browserSize.Width; i = i + 1 | 0)
                hiddeny[i] = (uint)browserSize.Height;

            double deltaz = 1.0;

            // Draw from front to back
            for (double z = 1; z < camera.distance; z += deltaz)
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
                double invz = 1.0 / z * 240.0;
                for (uint i = 0; i < browserSize.Width; i = i + 1)
                {
                    var mapoffset = (((int)(ply+0.5d) & mapwidthperiod) << (int)map.shift) + ((int)(plx + 0.5d) & mapheightperiod);
                    var heightonscreen = (camera.height - map.altitude[mapoffset]) * invz + camera.horizon;
                    DrawVerticalLine(i, (uint)heightonscreen, hiddeny[i], map.color[mapoffset]);
                    if (heightonscreen < hiddeny[i]) hiddeny[i] = (uint)heightonscreen;
                    plx += dx;
                    ply += dy;
                }
                deltaz += 0.005;
            }

            void DrawVerticalLine(uint x, uint ytop, uint ybottom, uint col)
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
    }
}
