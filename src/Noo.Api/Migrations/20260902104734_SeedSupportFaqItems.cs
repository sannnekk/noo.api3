using Microsoft.EntityFrameworkCore.Migrations;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Utils.Richtext.Tiptap;
using Noo.Api.Support.Types;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <summary>
    /// The frequently asked questions the help page opens with.
    /// </summary>
    /// <remarks>
    /// Content rather than schema, which migrations do not usually carry — but
    /// the help page has nowhere else to get a starting set from, and an empty
    /// FAQ on the day it ships is worse than one the support team edits down.
    /// The ids are fixed, so <c>Down</c> takes back exactly these rows and
    /// leaves anything written since alone.
    /// <para>
    /// The answers go through <see cref="RichTextJsonSerializer"/> rather than
    /// being written out as JSON, so what lands in the column is by construction
    /// what the reader deserialises.
    /// </para>
    /// </remarks>
    public partial class SeedSupportFaqItems : Migration
    {
        private const string _tableName = "support_faq_item";

        private static readonly string[] _columns =
            ["id", "question", "answer", "is_active", "category", "order"];

        /// <summary>
        /// Question, answer and category, in the order they are shown. The ids
        /// share a timestamp and differ only in their last character, so the
        /// seeded set is recognisable in the table and the same everywhere.
        /// </summary>
        private static readonly (
            string Id,
            string Question,
            string Answer,
            SupportCategory? Category
        )[] _items =
        [
            (
                "01M1FFRYR00000000000000001",
                "Не пришло письмо с доступом. Что делать?",
                "Проверьте папку «Спам» и убедитесь, что смотрите почту, указанную при покупке курса. Если письма нет и там — напишите нам в поддержку, мы откроем доступ вручную.",
                SupportCategory.Payment
            ),
            (
                "01M1FFRYR00000000000000002",
                "Забыл пароль. Как войти?",
                "На странице входа нажмите «Восстановить» рядом с «Забыли пароль?» и введите почту от аккаунта — на неё придёт ссылка для смены пароля.",
                null
            ),
            (
                "01M1FFRYR00000000000000003",
                "Как сдать домашнюю работу?",
                "Откройте работу в разделе «Работы», заполните ответы и нажмите «Сдать на проверку». Пока работа не сдана, ответы можно менять сколько угодно.",
                SupportCategory.Works
            ),
            (
                "01M1FFRYR00000000000000004",
                "Работа сдана, но оценки нет",
                "Работы проверяют кураторы вручную, поэтому оценка появляется не сразу. Если ждёте дольше обычного — напишите в поддержку, мы проверим, что работа дошла до куратора.",
                SupportCategory.Works
            ),
            (
                "01M1FFRYR00000000000000005",
                "Не открывается видео или конспект",
                "Обновите страницу и попробуйте другой браузер — чаще всего дело в расширениях или в старой версии браузера. Если не помогло, пришлите нам ссылку на материал и скриншот.",
                SupportCategory.Courses
            ),
            (
                "01M1FFRYR00000000000000006",
                "Можно ли заниматься с телефона?",
                "Да, платформа работает в мобильном браузере. Но длинные работы удобнее сдавать с компьютера — там больше места для ответов.",
                SupportCategory.Courses
            ),
            (
                "01M1FFRYR00000000000000007",
                "Как продлить доступ к курсу?",
                "Подписка продлевается на сайте школы, там же лежат все тарифы и скидки. После оплаты доступ на платформе обновляется автоматически.",
                SupportCategory.Payment
            ),
            (
                "01M1FFRYR00000000000000008",
                "Можно ли вернуть деньги за курс?",
                "Да, возврат оформляется по заявлению — условия описаны в договоре оферты. Напишите в поддержку, и мы подскажем, что приложить.",
                SupportCategory.Payment
            ),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            for (var index = 0; index < _items.Length; index++)
            {
                var item = _items[index];

                migrationBuilder.InsertData(
                    table: _tableName,
                    columns: _columns,
                    values:
                    [
                        Ulid.Parse(item.Id).ToByteArray(),
                        item.Question,
                        RichTextJsonSerializer.Serialize(
                            TiptapRichText.FromString(item.Answer)
                        ),
                        true,
                        item.Category?.ToString(),
                        index + 1
                    ]
                );
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var item in _items)
            {
                migrationBuilder.DeleteData(
                    table: _tableName,
                    keyColumn: "id",
                    keyValue: Ulid.Parse(item.Id).ToByteArray()
                );
            }
        }
    }
}
