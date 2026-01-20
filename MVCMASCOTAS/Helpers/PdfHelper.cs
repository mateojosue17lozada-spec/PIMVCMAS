using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using MVCMASCOTAS.Models;

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
                document.Add(new Paragraph($"Sexo: {mascota.Sexo}", normalFont));
                document.Add(new Paragraph($"Edad Aproximada: {mascota.EdadAproximada}", normalFont));
                document.Add(new Paragraph($"Tamaño: {mascota.Tamanio}", normalFont));
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

                // Fecha y firmas
                document.Add(new Paragraph($"Fecha de adopción: {contrato.FechaAdopcion:dd/MM/yyyy}", normalFont));
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
                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 15f, 25f, 20f, 20f, 20f });

                // Encabezados
                table.AddCell(new PdfPCell(new Phrase("Fecha", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("Donante", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("Tipo", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("Método", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                table.AddCell(new PdfPCell(new Phrase("Monto", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_RIGHT });

                // Datos
                decimal total = 0;
                foreach (var donacion in donaciones)
                {
                    table.AddCell(new Phrase(donacion.FechaDonacion.ToString("dd/MM/yyyy"), normalFont));
                    table.AddCell(new Phrase(donacion.NombreDonante ?? "Anónimo", normalFont));
                    table.AddCell(new Phrase(donacion.TipoDonacion, normalFont));
                    table.AddCell(new Phrase(donacion.MetodoPago ?? "-", normalFont));

                    decimal monto = donacion.MontoEfectivo ?? 0;
                    table.AddCell(new Phrase($"${monto:N2}", normalFont) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    total += monto;
                }

                // Fila de total
                PdfPCell totalCell = new PdfPCell(new Phrase("TOTAL:", headerFont));
                totalCell.Colspan = 4;
                totalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                totalCell.BackgroundColor = BaseColor.LIGHT_GRAY;
                table.AddCell(totalCell);

                table.AddCell(new PdfPCell(new Phrase($"${total:N2}", headerFont))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = BaseColor.LIGHT_GRAY
                });

                document.Add(table);

                document.Close();

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Genera un certificado de apadrinamiento en PDF
        /// </summary>
        public static byte[] GenerarCertificadoApadrinamiento(Apadrinamientos apadrinamiento, Usuarios padrino, Mascotas mascota)
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

                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(" "));

                // Título
                Paragraph title = new Paragraph("CERTIFICADO DE APADRINAMIENTO", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 30;
                document.Add(title);

                // Contenido
                Paragraph content = new Paragraph();
                content.Alignment = Element.ALIGN_CENTER;
                content.Add(new Chunk("El Refugio de Animales Quito certifica que\n\n", normalFont));
                content.Add(new Chunk(padrino.NombreCompleto + "\n\n", subtitleFont));
                content.Add(new Chunk("ha apadrinado a\n\n", normalFont));
                content.Add(new Chunk(mascota.Nombre + "\n\n", subtitleFont));
                content.Add(new Chunk($"({mascota.Especie} - {mascota.Raza ?? "Mestizo"})\n\n", normalFont));
                content.Add(new Chunk($"Desde el {apadrinamiento.FechaInicio:dd/MM/yyyy}\n\n", normalFont));
                content.Add(new Chunk("Gracias por tu generosidad y compromiso con nuestros animales.", normalFont));

                document.Add(content);

                document.Close();

                return ms.ToArray();
            }
        }
    }
}
