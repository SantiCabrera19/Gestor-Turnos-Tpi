using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpaAplicacionDesktop
{
    public partial class Form1 : Form
    {

        // Conexion a la base de datos mySQL 'gestion_turnos' desde el localhost
        string connectionString = "server=localhost;database=gestion_turnos;user=root;password=42331;";
        private int menuLateralPanelTargetWidth; // Ancho objetivo del panel
        private int menuLateralPanelStep; // Incremento/decremento del ancho en cada tick


        // Constructor principal por defecto
        public Form1()
        {
            InitializeComponent();
            CargarProfesionales();
            CargarServicios();
            CargarTurnosEnDataGridView();
            CargarDatosPago();
            MarcarFechasDeTurnos();
            verTurnosDataGrid.Click += new EventHandler(verTurnosDataGrid_Click);
        }


        // Método para abrir la conexión
        private MySqlConnection OpenConnection()
        {
            MySqlConnection conn = new MySqlConnection(connectionString);
            try
            {
                conn.Open();
                return conn;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error al conectar a la base de datos: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Todo esto es para Registrar Turno / Servicio 
        /// </summary>

        // Metodo para cargar el cliente a la base de datos con el boton CargarRegbutton
        private void CargarCliente()
        {
            // Abrir conexión a la base de datos
            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    // Recoger datos del formulario
                    string nombreCliente = NombretextBox.Text;
                    string apellidoCliente = apellidoTextbox.Text;
                    string direccionCliente = direccionMaskedTextBox.Text;
                    string emailCliente = EmailtextBox.Text;
                    string telefonoCliente = NroTelmaskedTextBox.Text;
                    DateTime fechaRegistro = RegdateTimePicker.Value;

                    // Validar que los campos obligatorios no estén vacíos
                    if (string.IsNullOrEmpty(nombreCliente) || string.IsNullOrEmpty(apellidoCliente) ||
                        string.IsNullOrEmpty(direccionCliente) || string.IsNullOrEmpty(emailCliente) ||
                        string.IsNullOrEmpty(telefonoCliente))
                    {
                        MessageBox.Show("Por favor, complete todos los campos obligatorios.");
                        return;
                    }

                    // Verificar si el cliente ya existe en la base de datos por su email
                    string queryVerificar = "SELECT COUNT(*) FROM Clientes WHERE email = @Email";
                    MySqlCommand cmdVerificar = new MySqlCommand(queryVerificar, conn);
                    cmdVerificar.Parameters.AddWithValue("@Email", emailCliente);

                    int count = Convert.ToInt32(cmdVerificar.ExecuteScalar());

                    if (count > 0)
                    {
                        MessageBox.Show("El cliente ya existe en la base de datos.");
                    }
                    else
                    {
                        // Insertar el cliente en la base de datos
                        string queryInsertar = "INSERT INTO Clientes (nombre, apellido, direccion, email, telefono, fecha_registro) " +
                                               "VALUES (@Nombre, @Apellido, @Direccion, @Email, @Telefono, @FechaRegistro)";
                        MySqlCommand cmdInsertar = new MySqlCommand(queryInsertar, conn);
                        cmdInsertar.Parameters.AddWithValue("@Nombre", nombreCliente);
                        cmdInsertar.Parameters.AddWithValue("@Apellido", apellidoCliente);
                        cmdInsertar.Parameters.AddWithValue("@Direccion", direccionCliente);
                        cmdInsertar.Parameters.AddWithValue("@Email", emailCliente);
                        cmdInsertar.Parameters.AddWithValue("@Telefono", telefonoCliente);
                        cmdInsertar.Parameters.AddWithValue("@FechaRegistro", fechaRegistro);

                        try
                        {
                            cmdInsertar.ExecuteNonQuery();
                            MessageBox.Show("Cliente registrado exitosamente.");
                        }
                        catch (MySqlException ex)
                        {
                            MessageBox.Show("Error al registrar el cliente: " + ex.Message);
                        }
                    }
                }
            }
        }
        // Método para cargar los profesionales en el comboBox ProComboBox
        private void CargarProfesionales()
        {
            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    string queryProfesionales = "SELECT CONCAT(nombre, ' ', apellido) AS nombre_completo FROM Profesionales";
                    MySqlCommand cmd = new MySqlCommand(queryProfesionales, conn);

                    try
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Cambia ProtextBox por ProComboBox si es un ComboBox
                                ProtextBox.Items.Add(reader["nombre_completo"].ToString());
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error al cargar profesionales: " + ex.Message);
                    }
                }
            }
        }
        // Método para cargar los servicios/productos en el comboBox ServProdcomboBox
        private void CargarServicios()
        {
            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    string queryServicios = "SELECT nombre_servicio FROM Servicios";
                    MySqlCommand cmd = new MySqlCommand(queryServicios, conn);

                    try
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Añadir cada servicio al comboBox
                                ServProdcomboBox.Items.Add(reader["nombre_servicio"].ToString());
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error al cargar servicios: " + ex.Message);
                    }
                }
            }
        }

        // evento Click del boton Registrar para cargar el cliente
        private void CargarRegbutton_Click_1(object sender, EventArgs e)
        {
            CargarCliente();
        }

        /// <summary>
        /// Todo esto es para INFORMACION DE TURNOS Y SERVICIOS y el CALENDARIO
        /// </summary>

        // evento que carga los turnos en el DataGridView
        private void CargarTurnosEnDataGridView()
        {
            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    string queryTurnos = @"
                SELECT 
                    c.nombre AS nombreCliente, 
                    s.nombre_servicio AS nombreServicio, 
                    CONCAT(p.nombre, ' ', p.apellido) AS nombreProfesional, 
                    t.hora_turno AS horario, 
                    t.estado_turno AS turnoEstado, 
                    c.email AS correoElectronico 
                FROM Turnos t
                JOIN Clientes c ON t.id_cliente = c.id_cliente
                JOIN Servicios s ON t.id_servicio = s.id_servicio
                JOIN Profesionales p ON t.id_profesional = p.id_profesional";

                    MySqlCommand cmd = new MySqlCommand(queryTurnos, conn);
                    try
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Limpiar el DataGridView antes de cargar nuevos datos
                            turnosDataGridView.Rows.Clear();
                            while (reader.Read())
                            {
                                turnosDataGridView.Rows.Add(
                                    reader["nombreCliente"].ToString(),
                                    reader["nombreServicio"].ToString(),
                                    reader["nombreProfesional"].ToString(),
                                    reader["horario"].ToString(),
                                    reader["turnoEstado"].ToString(),
                                    reader["correoElectronico"].ToString()
                                );
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error al cargar los turnos: " + ex.Message);
                    }
                }
            }
        }

        // Metodo para cargar y marcar los turnos en el calendario
        private void MarcarFechasDeTurnos()
        {
            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    // Consulta para obtener las fechas de los turnos
                    string queryFechasTurnos = "SELECT DISTINCT fecha_turno FROM Turnos";

                    MySqlCommand cmd = new MySqlCommand(queryFechasTurnos, conn);

                    try
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Limpiar cualquier fecha previamente marcada
                            calendarioTurnos.RemoveAllBoldedDates();

                            // Iterar por las fechas de los turnos y agregarlas como fechas marcadas
                            while (reader.Read())
                            {
                                DateTime fechaTurno = Convert.ToDateTime(reader["fecha_turno"]);

                                // Marcar la fecha en el calendario
                                calendarioTurnos.AddBoldedDate(fechaTurno);
                            }

                            // Refrescar el calendario para que se actualicen las fechas marcadas
                            calendarioTurnos.UpdateBoldedDates();
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error al obtener las fechas de los turnos: " + ex.Message);
                    }
                }
            }
        }

        // Método para cargar los turnos de una fecha específica en el DataGridView
        private void CargarTurnosPorFecha(DateTime fechaSeleccionada)
        {
            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    string queryTurnosPorFecha = @"
                SELECT 
                    c.nombre AS nombreCliente, 
                    s.nombre_servicio AS nombreServicio, 
                    CONCAT(p.nombre, ' ', p.apellido) AS nombreProfesional, 
                    t.hora_turno AS horario, 
                    t.estado_turno AS turnoEstado 
                FROM Turnos t
                JOIN Clientes c ON t.id_cliente = c.id_cliente
                JOIN Servicios s ON t.id_servicio = s.id_servicio
                JOIN Profesionales p ON t.id_profesional = p.id_profesional
                WHERE t.fecha_turno = @fecha_turno";

                    MySqlCommand cmd = new MySqlCommand(queryTurnosPorFecha, conn);
                    cmd.Parameters.AddWithValue("@fecha_turno", fechaSeleccionada.ToString("yyyy-MM-dd"));

                    try
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Limpiar el DataGridView antes de cargar los nuevos turnos
                            turnosDataGridView.Rows.Clear();

                            while (reader.Read())
                            {
                                turnosDataGridView.Rows.Add(
                                    reader["nombreCliente"].ToString(),
                                    reader["nombreServicio"].ToString(),
                                    reader["nombreProfesional"].ToString(),
                                    reader["horario"].ToString(),
                                    reader["turnoEstado"].ToString()
                                );
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error al cargar los turnos: " + ex.Message);
                    }
                }
            }
        }

        // Evento que se ejecuta cuando se selecciona una fecha en el calendario
        private void calendarioTurnos_DateChanged(object sender, DateRangeEventArgs e)
        {
            DateTime fechaSeleccionada = calendarioTurnos.SelectionStart;
            CargarTurnosPorFecha(fechaSeleccionada); // Llama al método cuando cambie la fecha
        }

        // Boton que muestra los turnos de nuevo
        private void verTurnosDataGrid_Click(object sender, EventArgs e)
        {
            CargarTurnosEnDataGridView();
        }

        /// <summary>
        /// Todo esto es para REGISTRAR LOS PAGOS
        /// </summary>


        // metodo CargarDatosPago para cargar los clientes y metodos de pago
        private void CargarDatosPago()
        {
            CargarClientes();
            CargarMetPago();
            CargarTurnos();
        }

        // metodo para cargar los clientes en el comboBox PickCliente
        private void CargarClientes()
        {
            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    string queryClientes = "SELECT id_cliente, CONCAT(nombre, ' ', apellido) AS nombre_completo FROM Clientes";
                    MySqlCommand cmd = new MySqlCommand(queryClientes, conn);

                    try
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Limpiar el comboBox antes de añadir nuevos items
                            PickCliente.Items.Clear();

                            while (reader.Read())
                            {
                                PickCliente.Items.Add(new { Text = reader["nombre_completo"].ToString(), Value = reader["id_cliente"] });
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error al cargar clientes: " + ex.Message);
                    }
                }
            }
        }

        // metodo para cargar los metodos de pago en el comboBox PickPago
        private void CargarMetPago()
        {
            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    string queryMetodosPago = "SELECT nombre_metodo FROM Metodos_Pago";
                    MySqlCommand cmd = new MySqlCommand(queryMetodosPago, conn);

                    try
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Limpiar el comboBox antes de añadir nuevos items
                            PickPago.Items.Clear();

                            while (reader.Read())
                            {
                                PickPago.Items.Add(reader["nombre_metodo"].ToString());
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error al cargar métodos de pago: " + ex.Message);
                    }
                }
            }
        }

        // metodo para cargar los turnos en el comboBox PickTurno
        private void CargarTurnos()
        {
            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    string queryTurnos = "SELECT id_turno, CONCAT(fecha_turno, ' ', hora_turno) AS turno_info FROM Turnos";
                    MySqlCommand cmd = new MySqlCommand(queryTurnos, conn);

                    try
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Limpiar el comboBox antes de añadir nuevos items
                            PickTurno.Items.Clear();

                            while (reader.Read())
                            {
                                PickTurno.Items.Add(new { Text = reader["turno_info"].ToString(), Value = reader["id_turno"] });
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error al cargar turnos: " + ex.Message);
                    }
                }
            }
        }

        // Metodo para confirmar el pago y enviar finalmente la informacion a la base de datos
        private void ConfirmarPago()
        {
            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    try
                    {
                        // Verificar y obtener el cliente seleccionado
                        var selectedCliente = PickCliente.SelectedItem as dynamic;
                        if (selectedCliente == null)
                        {
                            MessageBox.Show("Por favor, seleccione un cliente.");
                            return;
                        }
                        string id_cliente = selectedCliente.Value.ToString();


                        // Verificar y obtener el método de pago seleccionado
                        if (PickPago.SelectedItem == null)
                        {
                            MessageBox.Show("Por favor, seleccione un método de pago.");
                            return;
                        }
                        int id_metodo = PickPago.SelectedIndex + 1; // 1 es efectivo, 2 es transferencia

                        // Verificar y obtener el monto
                        if (string.IsNullOrEmpty(MontoTextBox.Text))
                        {
                            MessageBox.Show("Por favor, ingrese un monto válido.");
                            return;
                        }
                        decimal monto_pago;
                        if (!decimal.TryParse(MontoTextBox.Text, out monto_pago))
                        {
                            MessageBox.Show("Monto inválido. Por favor, ingrese un valor numérico.");
                            return;
                        }

                        // Verificar y obtener el turno seleccionado
                        var selectedTurno = PickTurno.SelectedItem as dynamic;
                        if (selectedTurno == null)
                        {
                            MessageBox.Show("Por favor, seleccione un turno.");
                            return;
                        }
                        int id_turno = selectedTurno.Value;

                        DateTime fecha_pago = FechaPagoDateTimeP.Value;

                        string queryInsertarPago = @"
                    INSERT INTO Pagos (fecha_pago, monto_pago, id_turno, id_metodo) 
                    VALUES (@fecha_pago, @monto_pago, @id_turno, @id_metodo)";

                        MySqlCommand cmd = new MySqlCommand(queryInsertarPago, conn);
                        cmd.Parameters.AddWithValue("@fecha_pago", fecha_pago);
                        cmd.Parameters.AddWithValue("@monto_pago", monto_pago);
                        cmd.Parameters.AddWithValue("@id_turno", id_turno);
                        cmd.Parameters.AddWithValue("@id_metodo", id_metodo);

                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Pago registrado exitosamente.");

                        if (enviarTurnoEmailCheck.Checked)
                        {
                            // Aquí puedes agregar la lógica para enviar el turno por email.
                            EnviarCorreo(id_cliente, monto_pago, fecha_pago);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al registrar el pago: " + ex.Message);
                    }
                }
                else
                {
                    MessageBox.Show("Error al conectar a la base de datos.");
                }
            }
        }

        // metodo que enviará el correo al cliente con los detalles del pago
        private void EnviarCorreo(string id_cliente, decimal monto_pago, DateTime fecha_pago)
        {
            // Implementa la lógica para enviar el correo aquí
            MessageBox.Show("Correo enviado al cliente con los detalles del pago.");
        }

        // evento Click del boton ConfirmarPagoBtn
        private void ConfirmarPagoBtn_Click(object sender, EventArgs e)
        {
            ConfirmarPago();
        }

        /// <summary>
        /// Evento para generar la factura basado en el último pago registrado
        /// </summary>
        private void generarFacturaBtn_Click(object sender, EventArgs e)
        {
            // 1. Obtener el último pago registrado
            string queryUltimoPago = @"
        SELECT pagos.id_pago, pagos.monto_pago, pagos.fecha_pago, pagos.id_cliente, clientes.nombre, clientes.apellido, clientes.email, clientes.telefono
        FROM pagos
        INNER JOIN clientes ON pagos.id_cliente = clientes.id_cliente
        ORDER BY pagos.fecha_pago DESC
        LIMIT 1;";

            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    MySqlCommand cmd = new MySqlCommand(queryUltimoPago, conn);

                    try
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // 2. Obtener los datos del último pago y cliente
                                decimal totalMonto = reader.GetDecimal("monto_pago");
                                DateTime fechaPago = reader.GetDateTime("fecha_pago");
                                string nombreCliente = reader.GetString("nombre") + " " + reader.GetString("apellido");
                                string emailCliente = reader.GetString("email");
                                string nroTelefono = reader.GetString("telefono");

                                // 3. Llenar los labels con la información obtenida
                                LlenarFactura(nombreCliente, emailCliente, fechaPago, nroTelefono, totalMonto);

                                MessageBox.Show("Factura generada correctamente.");
                            }
                            else
                            {
                                MessageBox.Show("No se encontró información de pagos recientes.");
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error al generar la factura: " + ex.Message);
                    }
                }
            }
        }

        // Método para llenar los labels de la factura

        private void LlenarFactura(string nombreClienteData, string emailClienteData, DateTime fechaPago, string nroTelefonoData, decimal totalMontoData)
        {
            // Asignar valores a los labels de la factura (labels azules)
            nombreCliente.Text = nombreClienteData;  // nombreCliente es el control Label de la UI
            emailCliente.Text = emailClienteData;    // emailCliente es el control Label de la UI
            fechaHora.Text = fechaPago.ToString("dd/MM/yyyy HH:mm"); // Formatear la fecha y hora del pago
            nroTelefono.Text = nroTelefonoData;      // nroTelefono es el control Label de la UI
            totalMonto.Text = $"${totalMontoData:N2}"; // Formato de moneda
            infoPago.Text = "Efectivo"; // Esto lo puedes ajustar según sea necesario
        }

        /// <summary>
        /// INFORMES DE LOS PAGOS.. en esta seccion se trabajara con la tabla 'pagos' y el campo montos..
        /// </summary>
        /// 

        // Definir la clase PaymentInfo
        public class PaymentInfo
        {
            public DateTime FechaPago { get; set; }
            public string MetodoPago { get; set; }
            public decimal Monto { get; set; }
        }

        

        // primero un metodo para validar fechas
        private bool ValidarFechas(DateTime fechaInicio, DateTime fechaFin)
        {
            if (fechaInicio < new DateTime(2024, 1, 1) || fechaFin < new DateTime(2024, 1, 1))
            {
                MessageBox.Show("Las fechas deben ser posteriores a enero 2024.");
                return false;
            }

            if (fechaInicio > fechaFin)
            {
                MessageBox.Show("La fecha de inicio no puede ser posterior a la fecha de fin.");
                return false;
            }

            return true;
        }

        // un metodo para obtener los pagos, pero en el rango de fechas seleccionado
        private List<PaymentInfo> ObtenerDatosPagos(DateTime fechaInicio, DateTime fechaFin)
        {
            List<PaymentInfo> pagos = new List<PaymentInfo>();

            using (MySqlConnection conn = OpenConnection())
            {
                if (conn != null)
                {
                    string queryPagos = @"
                SELECT fecha_pago, nombre_metodo AS infoMetodoPago, monto_pago AS Monto
                FROM Pagos p
                JOIN Metodos_Pago m ON p.id_metodo = m.id_metodo
                WHERE fecha_pago BETWEEN @fechaInicio AND @fechaFin";

                    MySqlCommand cmd = new MySqlCommand(queryPagos, conn);
                    cmd.Parameters.AddWithValue("@fechaInicio", fechaInicio);
                    cmd.Parameters.AddWithValue("@fechaFin", fechaFin);

                    try
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                pagos.Add(new PaymentInfo
                                {
                                    FechaPago = reader.GetDateTime("fecha_pago"),
                                    MetodoPago = reader.GetString("infoMetodoPago"),
                                    Monto = reader.GetDecimal("Monto"),
                                });
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error al obtener los datos de los pagos: " + ex.Message);
                    }
                }
            }

            return pagos;
        }

        // un metodo para mostrar los datos en el DataGridView
        private void MostrarDatosEnDataGridView(List<PaymentInfo> pagos)
        {
            InformeDataGrid.Rows.Clear();

            foreach (var pago in pagos)
            {
                InformeDataGrid.Rows.Add(
                    pago.FechaPago.ToString("yyyy-MM-dd HH:mm"),
                    pago.MetodoPago,
                    pago.Monto
                );
            }

            if (pagos.Count == 0)
            {
                MessageBox.Show("No hay pagos registrados en el rango de fechas seleccionado.");
            }
        }

        // finalmente un metodo para generar el informe
        private void GenerarInforme()
        {
            DateTime fechaInicio = FechaInicioDatePicker.Value.Date;
            DateTime fechaFin = FechaFinDatePicker.Value.Date;

            if (!ValidarFechas(fechaInicio, fechaFin))
            {
                return;
            }

            List<PaymentInfo> pagos = ObtenerDatosPagos(fechaInicio, fechaFin);
            MostrarDatosEnDataGridView(pagos);
        }

        private void generarInformeBtn_Click(object sender, EventArgs e)
        {
            GenerarInforme();
        }



        // boton que controla el menu lateral "menuLateralPanel", hace que se oculta o se muestre, segun el estado actual
        private void mostrarOcultarMenuButton_Click(object sender, EventArgs e)
        {
            if (menuLateralPanel.Width == 250)
            {
                menuLateralPanelTargetWidth = 60; // Ancho objetivo al ocultar
                menuLateralPanelStep = -10; // Decrementar el ancho en 10 píxeles en cada tick
            }
            else
            {
                menuLateralPanelTargetWidth = 250; // Ancho objetivo al mostrar
                menuLateralPanelStep = 10; // Incrementar el ancho en 10 píxeles en cada tick
            }

            animacionMenuTimer.Start(); // Iniciar el temporizador
        }

        private void animacionMenuTimer_Tick(object sender, EventArgs e)
        {
            // Modificar el ancho del panel
            menuLateralPanel.Width += menuLateralPanelStep;

            // Detener la animación cuando se alcanza el ancho objetivo
            if (
              (menuLateralPanelStep > 0 && menuLateralPanel.Width >= menuLateralPanelTargetWidth) ||
              (menuLateralPanelStep < 0 && menuLateralPanel.Width <= menuLateralPanelTargetWidth)
            )
            {
                menuLateralPanel.Width = menuLateralPanelTargetWidth;
                animacionMenuTimer.Stop();
            }
        }
    }
}
