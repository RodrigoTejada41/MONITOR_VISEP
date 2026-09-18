using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Visep.Desktop
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string path = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Visep", "data.xml");
            try
            {
                Store store = new Store(path);
                using (LoginForm login = new LoginForm(store, path))
                    if (login.ShowDialog() == DialogResult.OK) Application.Run(new MainForm(store, login.Session, path));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Falha ao iniciar", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }

    internal class WorkForm : Form
    {
        protected async Task Run(Action work, Action complete)
        {
            Enabled = false;
            try { await Task.Run(work); if (!IsDisposed && complete != null) complete(); }
            catch (Exception ex) { if (!IsDisposed) MessageBox.Show(this, ex.Message, "Operacao nao concluida", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { if (!IsDisposed) Enabled = true; }
        }
        protected static TextBox Field(TableLayoutPanel panel, string label, bool password)
        {
            int row = panel.RowCount++;
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(4, 9, 4, 4) }, 0, row);
            TextBox box = new TextBox { Name = label, AccessibleName = label, Dock = DockStyle.Top, UseSystemPasswordChar = password, MaxLength = 4000 };
            panel.Controls.Add(box, 1, row);
            return box;
        }
        protected static TableLayoutPanel Fields()
        {
            TableLayoutPanel panel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(12) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return panel;
        }
        protected static Button Button(string text, EventHandler handler)
        {
            Button button = new Button { Name = text, AccessibleName = text, Text = text, AutoSize = true, MinimumSize = new Size(110, 32), Margin = new Padding(4) };
            button.Click += handler;
            return button;
        }
    }

    internal sealed class LoginForm : WorkForm
    {
        public Session Session { get; private set; }
        public LoginForm(Store store, string path)
        {
            bool firstAccess = !store.HasUsers;
            Text = "VISEP - Autenticacao"; Width = 620; Height = 310;
            StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            TableLayoutPanel fields = Fields();
            TextBox user = Field(fields, "Usuario", false);
            TextBox password = Field(fields, "Senha", true);
            TextBox confirm = firstAccess ? Field(fields, "Confirmacao (primeiro acesso)", true) : null;
            TextBox database = Field(fields, "Base de dados", false); database.Text = path; database.ReadOnly = true;
            Label status = new Label { Dock = DockStyle.Bottom, Height = 45, Text = firstAccess ? "Primeiro acesso: crie administrador. Senha: 12 a 256 caracteres." : "Informe seu usuario e senha cadastrados." };
            Button enter = Button(firstAccess ? "Criar administrador" : "Entrar", async delegate
            {
                string username = user.Text.Trim(), secret = password.Text, confirmation = confirm == null ? "" : confirm.Text;
                await Run(delegate
                {
                    if (!store.HasUsers)
                    {
                        if (!firstAccess) throw new InvalidOperationException("Base sem usuarios. Feche e abra novamente para verificar o primeiro acesso.");
                        if (secret != confirmation) throw new InvalidOperationException("Confirmacao da senha diferente.");
                        store.Bootstrap(username, secret);
                    }
                    Session = store.Login(username, secret);
                }, delegate { password.Clear(); if (confirm != null) confirm.Clear(); DialogResult = DialogResult.OK; Close(); });
            });
            enter.Dock = DockStyle.Bottom; AcceptButton = enter;
            Controls.Add(fields); Controls.Add(enter); Controls.Add(status);
        }
    }

    internal sealed class MainForm : WorkForm
    {
        private readonly Store store;
        private readonly Session session;
        private readonly string dataPath;
        private readonly DataGridView grid = new DataGridView();
        private readonly TextBox filter = new TextBox();
        private readonly TextBox detail = new TextBox();
        private readonly Label status = new Label();
        private readonly CheckBox sound = new CheckBox { Text = "Aviso sonoro", AutoSize = true };
        private readonly Timer timer = new Timer { Interval = 3000 };
        private bool loading;
        private int previousCount = -1;

        public MainForm(Store store, Session session, string path)
        {
            this.store = store; this.session = session; dataPath = path;
            Text = "VISEP - " + session.User + " (" + session.Role + ")"; Width = 1180; Height = 760; MinimumSize = new Size(900, 600);
            Label banner = new Label { Dock = DockStyle.Top, Height = 35, BackColor = Color.DarkOrange, Text = "SIMULACAO / HOMOLOGACAO - sem conexao com centrais reais", TextAlign = ContentAlignment.MiddleCenter };
            TabControl tabs = new TabControl { Dock = DockStyle.Fill };
            TabPage events = new TabPage("Ocorrencias"); tabs.TabPages.Add(events);
            grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false; grid.AutoGenerateColumns = true;
            grid.SelectionChanged += delegate { ShowDetail(); };
            detail.Dock = DockStyle.Bottom; detail.Height = 155; detail.Multiline = true; detail.ReadOnly = true; detail.ScrollBars = ScrollBars.Vertical;
            FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
            filter.Width = 160; filter.AccessibleName = "Filtro de ocorrencias"; filter.Name = "Filtro";
            actions.Controls.Add(new Label { Text = "Filtro", AutoSize = true }); actions.Controls.Add(filter);
            actions.Controls.Add(Button("Atualizar", async delegate { await RefreshEvents(); }));
            actions.Controls.Add(Button("Assumir", async delegate { Incident item = Selected(); if (item != null) { await Run(delegate { store.Claim(session, item.Id); }, null); await RefreshEvents(); } }));
            actions.Controls.Add(Button("Registrar acao", async delegate { await ChangeIncident(false); }));
            actions.Controls.Add(Button("Encerrar", async delegate { await ChangeIncident(true); })); actions.Controls.Add(sound);
            events.Controls.Add(grid); events.Controls.Add(detail); events.Controls.Add(actions);
            AddClients(tabs); AddLegacyHistory(tabs); AddSimulation(tabs); AddAdministration(tabs);
            status.Dock = DockStyle.Bottom; status.Height = 26;
            TextBox database = new TextBox { Name = "Base de dados", AccessibleName = "Base de dados", Dock = DockStyle.Bottom, ReadOnly = true, Text = path };
            Controls.Add(tabs); Controls.Add(status); Controls.Add(database); Controls.Add(banner);
            timer.Tick += async delegate { await RefreshEvents(); };
            Shown += async delegate { await RefreshEvents(); timer.Start(); };
            FormClosed += delegate { timer.Stop(); timer.Dispose(); };
        }

        private Incident Selected() { return grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as Incident; }
        private void ShowDetail()
        {
            Incident item = Selected();
            detail.Text = item == null ? "" : "Conta: " + item.Account + " | Cliente: " + item.ClientName + "\r\nRecebido UTC: " + item.ReceivedUtc + " | Origem UTC: " + item.OriginUtc + "\r\nEvento bruto: " + item.Raw + "\r\nHistorico:\r\n" + item.Actions;
        }
        private async Task RefreshEvents()
        {
            if (loading || !Enabled || IsDisposed) return;
            loading = true;
            try
            {
                string search = filter.Text.Trim(); Incident selected = Selected();
                var items = await Task.Run(() => store.Incidents(session));
                if (IsDisposed) return;
                if (sound.Checked && previousCount >= 0 && items.Count > previousCount) System.Media.SystemSounds.Exclamation.Play();
                previousCount = items.Count;
                grid.DataSource = items.Where(x => search.Length == 0 || (x.Account + " " + x.ClientName + " " + x.Code + " " + x.Status + " " + x.Owner).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                foreach (string name in new[] { "Raw", "Actions" }) if (grid.Columns.Contains(name)) grid.Columns[name].Visible = false;
                if (selected != null) foreach (DataGridViewRow row in grid.Rows) if (((Incident)row.DataBoundItem).Id == selected.Id) { grid.CurrentCell = row.Cells[0]; break; }
                status.Text = items.Count + " ocorrencias | Atualizado " + DateTime.Now.ToString("HH:mm:ss") + " | Horarios de eventos em UTC";
            }
            catch (Exception ex) { if (!IsDisposed) status.Text = "Falha de leitura: " + ex.Message; }
            finally { loading = false; }
        }
        private async Task ChangeIncident(bool close)
        {
            Incident item = Selected(); if (item == null) return;
            string text = Ask(close ? "Justificativa de encerramento" : "Acao operacional"); if (text == null) return;
            await Run(delegate { if (close) store.Close(session, item.Id, text); else store.AddAction(session, item.Id, text); }, null);
            await RefreshEvents();
        }
        private string Ask(string title)
        {
            using (Form prompt = new Form { Text = title, Width = 580, Height = 270, StartPosition = FormStartPosition.CenterParent })
            {
                TextBox input = new TextBox { Dock = DockStyle.Fill, Multiline = true, MaxLength = 4000 };
                Button ok = Button("Confirmar", delegate { if (!string.IsNullOrWhiteSpace(input.Text)) prompt.DialogResult = DialogResult.OK; }); ok.Dock = DockStyle.Bottom;
                prompt.Controls.Add(input); prompt.Controls.Add(ok);
                return prompt.ShowDialog(this) == DialogResult.OK ? input.Text.Trim() : null;
            }
        }
        private void AddClients(TabControl tabs)
        {
            TabPage page = new TabPage("Clientes"); tabs.TabPages.Add(page);
            DataGridView clients = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoGenerateColumns = true };
            TableLayoutPanel fields = Fields();
            TextBox name = Field(fields, "Nome", false), account = Field(fields, "Conta", false), address = Field(fields, "Endereco", false), contacts = Field(fields, "Contatos (texto)", false), equipment = Field(fields, "Equipamentos (texto)", false), zones = Field(fields, "Zonas (texto)", false);
            FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45 };
            actions.Controls.Add(Button("Carregar clientes", async delegate { object result = null; await Run(delegate { result = store.Clients(session); }, delegate { clients.DataSource = result; }); }));
            actions.Controls.Add(Button("Cadastrar", async delegate
            {
                string n = name.Text, a = account.Text, ad = address.Text, c = contacts.Text, e = equipment.Text, z = zones.Text;
                await Run(delegate { store.AddClient(session, n, a, ad, c, e, z); }, delegate { MessageBox.Show(this, "Cliente cadastrado."); });
            }));
            page.Controls.Add(clients); page.Controls.Add(fields); page.Controls.Add(actions);
        }
        private void AddLegacyHistory(TabControl tabs)
        {
            TabPage page = new TabPage("Historico BYKOM"); tabs.TabPages.Add(page);
            DataGridView history = new DataGridView { Name = "Historico BYKOM", AccessibleName = "Historico BYKOM", Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = true };
            Label notice = new Label { Dock = DockStyle.Top, Height = 48, Text = "Historico importado - somente consulta; horario original sem conversao\r\nAmostra de ate 1000 eventos do backup. Nao representa o historico completo." };
            TableLayoutPanel fields = Fields();
            TextBox account = Field(fields, "Filtrar conta", false), client = Field(fields, "Filtrar cliente", false), code = Field(fields, "Filtrar codigo", false);
            Label count = new Label { AutoSize = true, Text = "Historico ainda nao carregado." };
            FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45 };
            actions.Controls.Add(Button("Carregar historico", async delegate
            {
                string a = account.Text.Trim(), c = client.Text.Trim(), e = code.Text.Trim();
                System.Collections.Generic.List<LegacyEvent> result = null;
                int total = 0;
                await Run(delegate
                {
                    var items = store.LegacyHistory(session); total = items.Count;
                    result = items.Where(x => x.Account.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0 && x.ClientName.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0 && x.Code.IndexOf(e, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }, delegate
                {
                    history.DataSource = result;
                    string[] names = { "Id", "SourceClientId", "Account", "ClientName", "Code", "Zone", "OccurredLocal", "Detail" };
                    string[] labels = { "ID legado", "ID cliente legado", "Conta", "Cliente", "Codigo", "Zona", "Horario original", "Detalhe" };
                    for (int i = 0; i < names.Length; i++) if (history.Columns.Contains(names[i])) history.Columns[names[i]].HeaderText = labels[i];
                    count.Text = result.Count + " exibidos / " + total + " importados (amostra)";
                });
            }));
            actions.Controls.Add(count);
            page.Controls.Add(history); page.Controls.Add(fields); page.Controls.Add(notice); page.Controls.Add(actions);
        }
        private void AddSimulation(TabControl tabs)
        {
            TabPage page = new TabPage("Simulador"); tabs.TabPages.Add(page); TableLayoutPanel fields = Fields();
            TextBox account = Field(fields, "Conta cadastrada", false), code = Field(fields, "Codigo", false), zone = Field(fields, "Zona", false), partition = Field(fields, "Particao", false);
            FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 90 };
            actions.Controls.Add(new Label { Text = "Requer receptor/servico ativo. Eventos ficam na fila mesmo com esta interface fechada.", AutoSize = true });
            actions.Controls.Add(Button("Enviar simulacao", async delegate
            {
                string a = account.Text.Trim(), c = code.Text.Trim(), z = zone.Text.Trim(), p = partition.Text.Trim();
                await Run(delegate
                {
                    if (a.Length == 0 || c.Length == 0 || z.Length == 0 || p.Length == 0) throw new InvalidOperationException("Informe conta, codigo, zona e particao.");
                    string inbox = Path.Combine(Path.GetDirectoryName(dataPath), "inbox"); Directory.CreateDirectory(inbox);
                    string id = Guid.NewGuid().ToString("N"); string temp = Path.Combine(inbox, id + ".tmp");
                    new XDocument(new XElement("simulation", new XAttribute("id", id), new XAttribute("account", a), new XAttribute("code", c), new XAttribute("zone", z), new XAttribute("partition", p))).Save(temp);
                    File.Move(temp, Path.Combine(inbox, id + ".xml"));
                }, delegate { MessageBox.Show(this, "Simulacao colocada na fila. Aguarde o receptor processar."); });
            })); page.Controls.Add(fields); page.Controls.Add(actions);
        }
        private void AddAdministration(TabControl tabs)
        {
            TabPage page = new TabPage("Administracao / Auditoria"); tabs.TabPages.Add(page);
            TableLayoutPanel fields = Fields(); TextBox user = Field(fields, "Novo usuario", false), password = Field(fields, "Senha", true), role = Field(fields, "Perfil (Admin/Operator/Viewer)", false); role.Text = "Operator";
            DataGridView audit = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false };
            FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45 };
            actions.Controls.Add(Button("Criar usuario", async delegate { string u = user.Text.Trim(), p = password.Text, r = role.Text.Trim(); await Run(delegate { store.AddUser(session, u, p, r); }, delegate { password.Clear(); MessageBox.Show(this, "Usuario criado."); }); }));
            actions.Controls.Add(Button("Ler auditoria", async delegate { object result = null; await Run(delegate { result = store.Audit(session); }, delegate { audit.DataSource = result; }); }));
            actions.Controls.Add(Button("Exportar CSV", async delegate { using (SaveFileDialog dialog = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "ocorrencias.csv" }) if (dialog.ShowDialog(this) == DialogResult.OK) { string path = dialog.FileName; await Run(delegate { store.ExportCsv(session, path); }, delegate { MessageBox.Show(this, "CSV exportado."); }); } }));
            page.Controls.Add(audit); page.Controls.Add(fields); page.Controls.Add(actions);
        }
    }
}
