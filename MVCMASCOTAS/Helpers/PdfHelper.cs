using iTextSharp.text;
using iTextSharp.text.pdf;
using MVCMASCOTAS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Helper para generación de documentos PDF usando iTextSharp
    /// </summary>
    public static class PdfHelper
    {
        /// <summary>
        /// Genera un PDF de contrato de adopción
        /// </summary>
        public static byte[] GenerarContratoAdopcion(ContratoAdopcion contrato, Usuarios adoptante, Mascotas mascota)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 50, 50, 50, 50);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // Fuentes
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                Font smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                // Título
                Paragraph title = new Paragraph("CONTRATO DE ADOPCIÓN", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20;
                document.Add(title);

                // Información del adoptante
                Paragraph adoptanteHeader = new Paragraph("DATOS DEL ADOPTANTE", headerFont);
                adoptanteHeader.SpacingAfter = 10;
                document.Add(adoptanteHeader);

                document.Add(new Paragraph($"Nombre: {adoptante.NombreCompleto}", normalFont));
                document.Add(new Paragraph($"Cédula: {adoptante.Cedula}", normalFont));
                document.Add(new Paragraph($"Email: {adoptante.Email}", normalFont));
                document.Add(new Paragraph($"Teléfono: {adoptante.Telefono}", normalFont));
                document.Add(new Paragraph($"Dirección: {adoptante.Direccion}", normalFont));
                document.Add(new Paragraph(" ")); // Espacio

                // Información de la mascota
                Paragraph mascotaHeader = new Paragraph("DATOS DE LA MASCOTA", headerFont);
                mascotaHeader.SpacingAfter = 10;
                document.Add(mascotaHeader);

                document.Add(new Paragraph($"Nombre: {mascota.Nombre}", normalFont));
                document.Add(new Paragraph($"Especie: {mascota.Especie}", normalFont));
                document.Add(new Paragraph($"Raza: {mascota.Raza ?? "Mestizo"}", normalFont));
                document.Add(new Paragraph($"Sexo: {(mascota.Sexo == "M" ? "Macho" : "Hembra")}", normalFont));
                document.Add(new Paragraph($"Edad Aproximada: {mascota.EdadAproximada}", normalFont));
                document.Add(new Paragraph($"Tamaño: {mascota.Tamanio}", normalFont));
                document.Add(new Paragraph($"Microchip: {mascota.Microchip ?? "No tiene"}", normalFont));
                document.Add(new Paragraph($"Esterilizado: {(mascota.Esterilizado == true ? "Sí" : "No")}", normalFont));
                document.Add(new Paragraph(" ")); // Espacio

                // Cláusulas del contrato
                Paragraph clausulasHeader = new Paragraph("CLÁUSULAS DEL CONTRATO", headerFont);
                clausulasHeader.SpacingAfter = 10;
                document.Add(clausulasHeader);

                string[] clausulas = {
                    "1. El adoptante se compromete a proporcionar cuidados veterinarios adecuados, incluyendo vacunación y desparasitación.",
                    "2. El adoptante se compromete a esterilizar a la mascota si aún no lo está, dentro de los 3 meses siguientes a la adopción.",
                    "3. El adoptante permitirá visitas de seguimiento por parte del refugio durante el primer año.",
                    "4. El adoptante se compromete a no abandonar, maltratar o regalar la mascota bajo ninguna circunstancia.",
                    "5. En caso de no poder continuar cuidando a la mascota, el adoptante debe devolverla al refugio.",
                    "6. El adoptante proporcionará alimentación adecuada, espacio suficiente y atención médica cuando sea necesario.",
                    "7. El adoptante acepta que la mascota vivirá en condiciones dignas y seguras.",
                    "8. El refugio se reserva el derecho de recuperar la mascota si se incumplen las condiciones del contrato."
                };

                foreach (string clausula in clausulas)
                {
                    Paragraph p = new Paragraph(clausula, normalFont);
                    p.SpacingAfter = 8;
                    document.Add(p);
                }

                document.Add(new Paragraph(" ")); // Espacio

                // Fecha y firmas - Usar fecha actual o fecha del contrato si existe
                DateTime fechaContrato = contrato.FechaContrato ?? DateTime.Now;
                document.Add(new Paragraph($"Fecha de adopción: {fechaContrato:dd/MM/yyyy}", normalFont));
                document.Add(new Paragraph(" ")); // Espacio
                document.Add(new Paragraph(" ")); // Espacio

                // Líneas de firma
                Paragraph firma1 = new Paragraph("_________________________", normalFont);
                firma1.Alignment = Element.ALIGN_CENTER;
                document.Add(firma1);

                Paragraph firmaAdoptante = new Paragraph("Firma del Adoptante", smallFont);
                firmaAdoptante.Alignment = Element.ALIGN_CENTER;
                document.Add(firmaAdoptante);

                document.Add(new Paragraph(" ")); // Espacio

                Paragraph firma2 = new Paragraph("_________________________", normalFont);
                firma2.Alignment = Element.ALIGN_CENTER;
                document.Add(firma2);

                Paragraph firmaRefugio = new Paragraph("Representante del Refugio", smallFont);
                firmaRefugio.Alignment = Element.ALIGN_CENTER;
                document.Add(firmaRefugio);

                document.Close();

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Genera un reporte de donaciones en PDF
        /// </summary>
        public static byte[] GenerarReporteDonaciones(List<Donaciones> donaciones, DateTime fechaInicio, DateTime fechaFin)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 30, 30, 30, 30);
                PdfWriter.GetInstance(document, ms);

                document.Open();

                // Fuentes
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);

                // Título
                Paragraph title = new Paragraph("REPORTE DE DONACIONES", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 15;
                document.Add(title);

                document.Add(new Paragraph($"Período: {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}", normalFont));
                document.Add(new Paragraph(" "));

                // Tabla
                PdfPTable table = new PdfPTable(6);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 10f, 20f, 15f, 15f, 15f, 25f });

                // Encabezados
                PdfPCell cell1 = new PdfPCell(new Phrase("Fecha", headerFont))
                {
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(cell1);

                PdfPCell cell2 = new PdfPCell(new Phrase("ID", headerFont))
                {
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(cell2);

                PdfPCell cell3 = new PdfPCell(new Phrase("Tipo", headerFont))
                {
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(cell3);

                PdfPCell cell4 = new PdfPCell(new Phrase("Método", headerFont))
                {
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(cell4);

                PdfPCell cell5 = new PdfPCell(new Phrase("Monto", headerFont))
                {
                    BackgroundColor = BaseColor.LIGHT_GRAY,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                };
                table.AddCell(cell5);

                PdfPCell cell6 = new PdfPCell(new Phrase("Estado", headerFont))
                {
                    BackgroundColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(cell6);

                // Datos
                decimal total = 0;
                foreach (var donacion in donaciones)
                {
                    // Fecha
                    string fechaStr = donacion.FechaDonacion.HasValue ?
                        donacion.FechaDonacion.Value.ToString("dd/MM/yyyy") : "Sin fecha";
                    table.AddCell(new Phrase(fechaStr, normalFont));

                    // ID
                    table.AddCell(new Phrase(donacion.DonacionId.ToString(), normalFont));

                    // Tipo
                    table.AddCell(new Phrase(donacion.TipoDonacion ?? "No especificado", normalFont));

                    // Método de pago
                    table.AddCell(new Phrase(donacion.MetodoPago ?? "-", normalFont));

                    // Monto (corregido el operador ?? para decimal)
                    decimal monto = donacion.Monto; // ya es decimal según tu DB
                    PdfPCell montoCell = new PdfPCell(new Phrase($"${monto:N2}", normalFont));
                    montoCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    table.AddCell(montoCell);
                    total += monto;

                    // Estado
                    table.AddCell(new Phrase(donacion.Estado ?? "Completada", normalFont));
                }

                // Fila de total
                PdfPCell totalLabelCell = new PdfPCell(new Phrase("TOTAL:", headerFont));
                totalLabelCell.Colspan = 5;
                totalLabelCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                totalLabelCell.BackgroundColor = BaseColor.LIGHT_GRAY;
                table.AddCell(totalLabelCell);

                PdfPCell totalValueCell = new PdfPCell(new Phrase($"${total:N2}", headerFont));
                totalValueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                totalValueCell.BackgroundColor = BaseColor.LIGHT_GRAY;
                table.AddCell(totalValueCell);

                document.Add(table);

                // Resumen
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph($"Total de donaciones: {donaciones.Count}", normalFont));
                document.Add(new Paragraph($"Monto total: ${total:N2}", headerFont));
                document.Add(new Paragraph($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}", normalFont));

                document.Close();

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Genera un certificado de apadrinamiento en PDF
        /// </summary>
        public static byte[] GenerarCertificadoApadrinamiento(Apadrinamientos apadrinamiento,
            Usuarios padrino, Mascotas mascota)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4.Rotate(), 50, 50, 50, 50);
                PdfWriter.GetInstance(document, ms);

                document.Open();

                // Fuentes
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 24);
                Font subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 14);
                Font smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(" "));

                // Título
                Paragraph title = new Paragraph("CERTIFICADO DE APADRINAMIENTO", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 30;
                document.Add(title);

                // Contenido principal
                Paragraph content = new Paragraph();
                content.Alignment = Element.ALIGN_CENTER;

                // Usar Add() con string y Font en lugar de Chunk
                content.Add(new Phrase("El Refugio de Animales Quito certifica que\n\n", normalFont));
                content.Add(new Phrase(padrino.NombreCompleto + "\n\n", subtitleFont));
                content.Add(new Phrase("ha apadrinado a\n\n", normalFont));
                content.Add(new Phrase(mascota.Nombre + "\n\n", subtitleFont));
                content.Add(new Phrase($"({mascota.Especie} - {mascota.Raza ?? "Mestizo"})\n\n", normalFont));

                string fechaInicio = apadrinamiento.FechaInicio.HasValue ?
                    apadrinamiento.FechaInicio.Value.ToString("dd/MM/yyyy") : "Fecha no especificada";
                content.Add(new Phrase($"Desde el {fechaInicio}\n\n", normalFont));
                content.Add(new Phrase("Gracias por tu generosidad y compromiso con nuestros animales.\n\n", normalFont));

                document.Add(content);

                // Información adicional
                Paragraph detalles = new Paragraph();
                detalles.Alignment = Element.ALIGN_CENTER;

                string montoMensual = apadrinamiento.MontoMensual.ToString("N2");
                string estado = apadrinamiento.Estado ?? "Activo";

                detalles.Add(new Phrase($"Monto mensual: ${montoMensual}\n", smallFont));
                detalles.Add(new Phrase($"Estado: {estado}\n", smallFont));

                if (apadrinamiento.FechaFin.HasValue)
                {
                    detalles.Add(new Phrase($"Fecha fin: {apadrinamiento.FechaFin.Value:dd/MM/yyyy}\n", smallFont));
                }

                detalles.Add(new Phrase($"ID Apadrinamiento: {apadrinamiento.ApadrinamientoId}\n", smallFont));
                detalles.Add(new Phrase($"Fecha de emisión: {DateTime.Now:dd/MM/yyyy}", smallFont));

                document.Add(detalles);

                document.Close();

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Genera un reporte de inventario de productos
        /// </summary>
        public static byte[] GenerarReporteInventario(List<Productos> productos)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4.Rotate(), 30, 30, 30, 30);
                PdfWriter.GetInstance(document, ms);

                document.Open();

                // Fuentes
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                Font alertFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.RED);

                // Título
                Paragraph title = new Paragraph("REPORTE DE INVENTARIO - TIENDA REFUGIO DE MASCOTAS", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 15;
                document.Add(title);

                document.Add(new Paragraph($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}", normalFont));
                document.Add(new Paragraph(" "));

                // Tabla
                PdfPTable table = new PdfPTable(8);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 10f, 20f, 15f, 15f, 10f, 10f, 10f, 10f });

                // Encabezados
                string[] headers = { "ID", "Producto", "Categoría", "SKU", "Precio", "Stock", "Mínimo", "Estado" };
                foreach (string header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    if (header == "Precio" || header == "Stock" || header == "Mínimo")
                        cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    table.AddCell(cell);
                }

                // Datos
                int productosBajoStock = 0;
                foreach (var producto in productos)
                {
                    // ID
                    table.AddCell(new Phrase(producto.ProductoId.ToString(), normalFont));

                    // Nombre del producto
                    table.AddCell(new Phrase(producto.NombreProducto, normalFont));

                    // Categoría (necesitarías obtener el nombre de la categoría)
                    table.AddCell(new Phrase(producto.CategoriaId.ToString(), normalFont));

                    // SKU
                    table.AddCell(new Phrase(producto.SKU ?? "Sin SKU", normalFont));

                    // Precio
                    PdfPCell precioCell = new PdfPCell(new Phrase($"${producto.Precio:N2}", normalFont));
                    precioCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    table.AddCell(precioCell);

                    // Stock
                    int stock = producto.Stock ?? 0;
                    Font stockFont = (stock <= (producto.StockMinimo ?? 0)) ? alertFont : normalFont;
                    PdfPCell stockCell = new PdfPCell(new Phrase(stock.ToString(), stockFont));
                    stockCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    table.AddCell(stockCell);

                    // Stock mínimo
                    PdfPCell minCell = new PdfPCell(new Phrase((producto.StockMinimo ?? 5).ToString(), normalFont));
                    minCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    table.AddCell(minCell);

                    // Estado
                    string estado = producto.Activo == true ? "Activo" : "Inactivo";
                    table.AddCell(new Phrase(estado, normalFont));

                    if (stock <= (producto.StockMinimo ?? 0))
                        productosBajoStock++;
                }

                document.Add(table);

                // Resumen
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph($"Total de productos: {productos.Count}", normalFont));
                document.Add(new Paragraph($"Productos bajo stock: {productosBajoStock}",
                    productosBajoStock > 0 ? alertFont : normalFont));
                document.Add(new Paragraph($"Productos activos: {productos.Count(p => p.Activo == true)}", normalFont));

                document.Close();

                return ms.ToArray();
            }
        }
    }
}