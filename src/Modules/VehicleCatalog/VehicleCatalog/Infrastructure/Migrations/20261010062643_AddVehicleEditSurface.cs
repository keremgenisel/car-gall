using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleEditSurface : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 0010b/D22 — plan bu kolonun `value_localized`'a YENİDEN ADLANDIRILMASINI
            // öngörüyordu ("veri kaybı yok"). Uygulamada tutmadı ve nedeni yazılmalı:
            // bugüne kadar yazılmış tek opsiyonel değer `damage_record_amount` ve o alan
            // artık `tutar` tipinde. Kolonda duran şey ise locale'li METİN ("12.500 TL").
            // Metni güvenilir biçimde `Money`'ye çeviren bir dönüşüm YOK — "12.500" hangi
            // ayracın binlik olduğunu söylemiyor. Yeniden adlandırmak, tipi `tutar` olan
            // bir alanda locale'li metin bırakırdı: sessiz ve kalıcı bir tutarsızlık.
            //
            // Bu kolona yazan tek yol bugüne kadar GELİŞTİRME SEED'iydi (ölçüldü: iki
            // çağrı yeri, biri test). Seed her kurulumda yeniden üretiliyor.
            migrationBuilder.DropColumn(
                name: "value",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "vehicle_catalog",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: false,
                // MEVCUT satırlar için `now()`. Üretilen varsayılan `0001-01-01`'di ve o
                // tarih listede "son güncellenen" sırasının en dibine düşer, ekranda da
                // `01.01.0001` olarak görünürdü. Gerçek güncelleme zamanı bilinmiyor;
                // bilinmeyeni geçmişin en başına yazmak, bilinmediğini söylemekten daha
                // kötü bir yalan olurdu. Uygulama her yazmada değeri kendisi veriyor.
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<decimal>(
                name: "value_amount",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "value_boolean",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "value_currency",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "value_date",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "value_integer",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "value_kind",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "value_localized",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "value_minor_unit",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "value_slug",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "value_text",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "vehicle_catalog",
                table: "listings",
                type: "jsonb",
                nullable: false,
                // Boş dize `jsonb`'ye çevrilemez (`invalid input syntax for type json`);
                // boş `LocalizedText`'in serileşmiş hâli `{}`.
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "title",
                schema: "vehicle_catalog",
                table: "listings",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "vehicle_catalog",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "value_amount",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.DropColumn(
                name: "value_boolean",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.DropColumn(
                name: "value_currency",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.DropColumn(
                name: "value_date",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.DropColumn(
                name: "value_integer",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.DropColumn(
                name: "value_kind",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.DropColumn(
                name: "value_localized",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.DropColumn(
                name: "value_minor_unit",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.DropColumn(
                name: "value_slug",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.DropColumn(
                name: "value_text",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "vehicle_catalog",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "title",
                schema: "vehicle_catalog",
                table: "listings");

            migrationBuilder.AddColumn<string>(
                name: "value",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }
    }
}
