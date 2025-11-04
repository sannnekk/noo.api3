using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "assigned_work_comment",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    content = table.Column<string>(type: "JSON", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assigned_work_comment", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "google_sheets_integration",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    name = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "ENUM('UserCourse', 'UserWork', 'UserRole', 'PollResults')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    selector_value = table.Column<string>(type: "VARCHAR(63)", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_run_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: true),
                    status = table.Column<string>(type: "ENUM('Active', 'Inactive', 'Error')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_error_text = table.Column<string>(type: "TEXT", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cron_pattern = table.Column<string>(type: "VARCHAR(63)", maxLength: 63, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    google_auth_data = table.Column<string>(type: "json", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    spreadsheet_id = table.Column<string>(type: "VARCHAR(127)", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_sheets_integration", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "media",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    hash = table.Column<string>(type: "VARCHAR(512)", maxLength: 512, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    path = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    actual_name = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    extension = table.Column<string>(type: "VARCHAR(15)", maxLength: 15, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    size = table.Column<int>(type: "INT(11)", nullable: false),
                    reason = table.Column<string>(type: "varchar(63)", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entity_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    owner_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    order = table.Column<int>(type: "INT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "poll",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    title = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "VARCHAR(512)", maxLength: 512, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: false),
                    is_auth_required = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "subject",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    name = table.Column<string>(type: "VARCHAR(63)", maxLength: 63, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    color = table.Column<string>(type: "VARCHAR(63)", maxLength: 63, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subject", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "support_category",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    name = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_pinned = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    parent_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    order = table.Column<int>(type: "INT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_category", x => x.id);
                    table.ForeignKey(
                        name: "FK_support_category_support_category_parent_id",
                        column: x => x.parent_id,
                        principalTable: "support_category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    name = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    username = table.Column<string>(type: "VARCHAR(63)", maxLength: 63, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "VARCHAR(255)", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    telegram_id = table.Column<string>(type: "VARCHAR(63)", maxLength: 63, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    telegram_username = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role = table.Column<string>(type: "ENUM('student', 'mentor', 'assistant', 'teacher', 'admin')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_blocked = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    is_verified = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "poll_question",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    poll_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    title = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "VARCHAR(512)", maxLength: 512, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_required = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    type = table.Column<string>(type: "ENUM('Checkbox', 'SingleChoice', 'MultipleChoice', 'Text', 'Number', 'Date', 'DateTime', 'Rating', 'Files')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    config = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    order = table.Column<int>(type: "INT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_question", x => x.id);
                    table.ForeignKey(
                        name: "FK_poll_question_poll_poll_id",
                        column: x => x.poll_id,
                        principalTable: "poll",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "course",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    name = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_date = table.Column<DateTime>(type: "DATETIME(0)", nullable: false),
                    end_date = table.Column<DateTime>(type: "DATETIME(0)", nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    thumbnail_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    subject_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    is_deleted = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_media_thumbnail_id",
                        column: x => x.thumbnail_id,
                        principalTable: "media",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_course_subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "work",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    title = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "ENUM('Test','MiniTest','Phrase','TrialWork','SecondPart')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    subject_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "support_article",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    title = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    content = table.Column<string>(type: "JSON", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    category_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    order = table.Column<int>(type: "INT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_article", x => x.id);
                    table.ForeignKey(
                        name: "FK_support_article_support_category_category_id",
                        column: x => x.category_id,
                        principalTable: "support_category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "mentor_assignment",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    mentor_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    student_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    subject_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mentor_assignment", x => x.id);
                    table.ForeignKey(
                        name: "FK_mentor_assignment_subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subject",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_mentor_assignment_user_mentor_id",
                        column: x => x.mentor_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mentor_assignment_user_student_id",
                        column: x => x.student_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "nootube_video",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    title = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "VARCHAR(512)", maxLength: 512, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    thumbnail_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    external_identifier = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    external_url = table.Column<string>(type: "VARCHAR(512)", maxLength: 512, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    external_thumbnail_url = table.Column<string>(type: "VARCHAR(512)", maxLength: 512, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    service_type = table.Column<string>(type: "ENUM('NooTubeServiceType', 'NooTube, YouTube, VkVideo, Rutube')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    state = table.Column<string>(type: "ENUM('VideoState', 'NotUploaded, Uploading, Uploaded, Published')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    duration = table.Column<uint>(type: "MEDIUMINT UNSIGNED", nullable: true),
                    published_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: true),
                    uploaded_by_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nootube_video", x => x.id);
                    table.ForeignKey(
                        name: "FK_nootube_video_media_thumbnail_id",
                        column: x => x.thumbnail_id,
                        principalTable: "media",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_nootube_video_user_uploaded_by_id",
                        column: x => x.uploaded_by_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    type = table.Column<string>(type: "VARCHAR(63)", maxLength: 63, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title = table.Column<string>(type: "VARCHAR(127)", maxLength: 127, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message = table.Column<string>(type: "VARCHAR(512)", maxLength: 512, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_read = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    is_banner = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    link = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    link_text = table.Column<string>(type: "VARCHAR(63)", maxLength: 63, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "poll_participation",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    poll_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    user_type = table.Column<string>(type: "ENUM('AuthenticatedUser', 'TelegramUser')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_external_identifier = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_external_data = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_participation", x => x.id);
                    table.ForeignKey(
                        name: "FK_poll_participation_poll_poll_id",
                        column: x => x.poll_id,
                        principalTable: "poll",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_poll_participation_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "session",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    ip_address = table.Column<string>(type: "VARCHAR(63)", maxLength: 63, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_id = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_agent = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_type = table.Column<string>(type: "ENUM('desktop', 'mobile', 'tablet', 'unknown')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    os = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    browser = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_request_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session", x => x.id);
                    table.ForeignKey(
                        name: "FK_session_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "snippet",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    name = table.Column<string>(type: "VARCHAR(63)", maxLength: 63, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    content = table.Column<string>(type: "JSON", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snippet", x => x.id);
                    table.ForeignKey(
                        name: "FK_snippet_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "user_avatar",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    avatar_type = table.Column<string>(type: "ENUM('None', 'Custom', 'Telegram')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    avatar_url = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    media_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_avatar", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_avatar_media_media_id",
                        column: x => x.media_id,
                        principalTable: "media",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_avatar_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    theme = table.Column<string>(type: "ENUM('light', 'dark', 'system')", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    font_size = table.Column<string>(type: "ENUM('small', 'normal', 'large')", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_settings_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "course_chapter",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    title = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    color = table.Column<string>(type: "VARCHAR(63)", maxLength: 63, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    course_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    parent_chapter_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    order = table.Column<int>(type: "INT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_chapter", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_chapter_course_chapter_parent_chapter_id",
                        column: x => x.parent_chapter_id,
                        principalTable: "course_chapter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_chapter_course_course_id",
                        column: x => x.course_id,
                        principalTable: "course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "course_membership",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    is_archived = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    course_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    student_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    assigner_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    type = table.Column<string>(type: "ENUM('ManualAssigned', 'ExternalAssigned', 'Subscription')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_membership", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_membership_course_course_id",
                        column: x => x.course_id,
                        principalTable: "course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_membership_user_assigner_id",
                        column: x => x.assigner_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_course_membership_user_student_id",
                        column: x => x.student_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "course_mm_CoursesAsAuthor_user",
                columns: table => new
                {
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    course_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_mm_CoursesAsAuthor_user", x => new { x.user_id, x.course_id });
                    table.ForeignKey(
                        name: "FK_course_mm_CoursesAsAuthor_user_course_course_id",
                        column: x => x.course_id,
                        principalTable: "course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_mm_CoursesAsAuthor_user_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "course_mm_CoursesAsEditor_user",
                columns: table => new
                {
                    course_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_mm_CoursesAsEditor_user", x => new { x.course_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_course_mm_CoursesAsEditor_user_course_course_id",
                        column: x => x.course_id,
                        principalTable: "course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_mm_CoursesAsEditor_user_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "assigned_work",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    title = table.Column<string>(type: "VARCHAR(512)", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "ENUM('Test','MiniTest','Phrase','TrialWork','SecondPart')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    attempt = table.Column<byte>(type: "TINYINT UNSIGNED", nullable: false),
                    solve_status = table.Column<string>(type: "ENUM('NotSolved', 'InProgress', 'Solved')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    solve_deadline_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: true),
                    is_solve_deadline_shifted = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    solved_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: true),
                    check_status = table.Column<string>(type: "ENUM('NotChecked', 'InProgress', 'Checked')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    check_deadline_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: true),
                    checked_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: true),
                    is_check_deadline_shifted = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    score = table.Column<ushort>(type: "SMALLINT UNSIGNED", nullable: true),
                    max_score = table.Column<ushort>(type: "SMALLINT UNSIGNED", nullable: false),
                    is_archived_by_student = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    is_archived_by_mentors = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    is_archived_by_assistants = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    ExcludedTaskIds = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    student_comment_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    main_mentor_comment_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    helper_mentor_comment_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    student_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    main_mentor_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    helper_mentor_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    work_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assigned_work", x => x.id);
                    table.ForeignKey(
                        name: "FK_assigned_work_assigned_work_comment_helper_mentor_comment_id",
                        column: x => x.helper_mentor_comment_id,
                        principalTable: "assigned_work_comment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_assigned_work_assigned_work_comment_main_mentor_comment_id",
                        column: x => x.main_mentor_comment_id,
                        principalTable: "assigned_work_comment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_assigned_work_assigned_work_comment_student_comment_id",
                        column: x => x.student_comment_id,
                        principalTable: "assigned_work_comment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_assigned_work_user_helper_mentor_id",
                        column: x => x.helper_mentor_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_assigned_work_user_main_mentor_id",
                        column: x => x.main_mentor_id,
                        principalTable: "user",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_assigned_work_user_student_id",
                        column: x => x.student_id,
                        principalTable: "user",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_assigned_work_work_work_id",
                        column: x => x.work_id,
                        principalTable: "work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "course_material_content",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    content = table.Column<string>(type: "JSON", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    work_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    is_work_available = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    work_solve_deadline_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: true),
                    work_check_deadline_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_material_content", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_material_content_work_work_id",
                        column: x => x.work_id,
                        principalTable: "work",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "work_task",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    content = table.Column<string>(type: "JSON", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    solve_hint = table.Column<string>(type: "JSON", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    explanation = table.Column<string>(type: "JSON", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    right_answers = table.Column<string>(type: "VARCHAR(512)", maxLength: 16, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "ENUM('Word','Text','Essay','FinalEssay')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    check_strategy = table.Column<string>(type: "ENUM('Manual', 'ExactMatchOrZero', 'ExactMatchWithWrongCharacter', 'MultipleChoice', 'Sequence')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    max_score = table.Column<byte>(type: "TINYINT UNSIGNED", nullable: false),
                    show_answer_before_check = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    check_one_by_one = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    work_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    order = table.Column<int>(type: "INT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_task", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_task_work_work_id",
                        column: x => x.work_id,
                        principalTable: "work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "nootube_video_comment",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    video_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    content = table.Column<string>(type: "VARCHAR(512)", maxLength: 512, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nootube_video_comment", x => x.id);
                    table.ForeignKey(
                        name: "FK_nootube_video_comment_nootube_video_video_id",
                        column: x => x.video_id,
                        principalTable: "nootube_video",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nootube_video_comment_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "nootube_video_reaction",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    video_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    reaction = table.Column<string>(type: "ENUM('VideoReaction', 'Like', 'Dislike', 'Heart', 'Laugh', 'Sad', 'Mindblowing')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nootube_video_reaction", x => x.id);
                    table.ForeignKey(
                        name: "FK_nootube_video_reaction_nootube_video_video_id",
                        column: x => x.video_id,
                        principalTable: "nootube_video",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nootube_video_reaction_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "poll_answer",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    poll_question_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    value = table.Column<string>(type: "json", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PollParticipationModelId = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_answer", x => x.id);
                    table.ForeignKey(
                        name: "FK_poll_answer_poll_participation_PollParticipationModelId",
                        column: x => x.PollParticipationModelId,
                        principalTable: "poll_participation",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_poll_answer_poll_question_poll_question_id",
                        column: x => x.poll_question_id,
                        principalTable: "poll_question",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "assigned_work_status_history",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    type = table.Column<string>(type: "ENUM('StartedSolving', 'SolveDeadlineShifted', 'Solved', 'StartedChecking', 'CheckDeadlineShifted', 'Checked', 'SentOnRecheck', 'SentOnResolve')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: false),
                    value = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    assigned_work_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    changed_by_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assigned_work_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_assigned_work_status_history_assigned_work_assigned_work_id",
                        column: x => x.assigned_work_id,
                        principalTable: "assigned_work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assigned_work_status_history_user_changed_by_id",
                        column: x => x.changed_by_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "calendar_event",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    assigned_work_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    type = table.Column<string>(type: "Enum('Custom', 'AssignedWorkCheckDeadline', 'AssignedWorkSolveDeadline', 'AssignedWorkChecked', 'AssignedWorkSolved')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "VARCHAR(512)", maxLength: 512, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_datetime = table.Column<DateTime>(type: "DATETIME(0)", nullable: false),
                    end_datetime = table.Column<DateTime>(type: "DATETIME(0)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calendar_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_calendar_event_assigned_work_assigned_work_id",
                        column: x => x.assigned_work_id,
                        principalTable: "assigned_work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_calendar_event_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "course_material",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    title = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title_color = table.Column<string>(type: "VARCHAR(63)", maxLength: 63, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    publish_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: true),
                    chapter_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    content_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    order = table.Column<int>(type: "INT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_material", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_material_course_chapter_chapter_id",
                        column: x => x.chapter_id,
                        principalTable: "course_chapter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_material_course_material_content_content_id",
                        column: x => x.content_id,
                        principalTable: "course_material_content",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "assigned_work_answer",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    rich_text_content = table.Column<string>(type: "JSON", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    word_content = table.Column<string>(type: "VARCHAR(63)", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mentor_comment = table.Column<string>(type: "JSON", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    score = table.Column<ushort>(type: "SMALLINT UNSIGNED", nullable: true),
                    max_score = table.Column<ushort>(type: "SMALLINT UNSIGNED", nullable: false),
                    status = table.Column<string>(type: "ENUM('NotSubmitted', 'Submitted')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    detailed_score = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    task_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    assigned_work_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    AssignedWorkModelId = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assigned_work_answer", x => x.id);
                    table.ForeignKey(
                        name: "FK_assigned_work_answer_assigned_work_AssignedWorkModelId",
                        column: x => x.AssignedWorkModelId,
                        principalTable: "assigned_work",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_assigned_work_answer_work_task_task_id",
                        column: x => x.task_id,
                        principalTable: "work_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "course_reaction",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    material_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    reaction = table.Column<string>(type: "ENUM('check', 'thinking')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_reaction", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_reaction_course_material_material_id",
                        column: x => x.material_id,
                        principalTable: "course_material",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_reaction_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_check_status",
                table: "assigned_work",
                column: "check_status");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_helper_mentor_comment_id",
                table: "assigned_work",
                column: "helper_mentor_comment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_helper_mentor_id",
                table: "assigned_work",
                column: "helper_mentor_id");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_main_mentor_comment_id",
                table: "assigned_work",
                column: "main_mentor_comment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_main_mentor_id",
                table: "assigned_work",
                column: "main_mentor_id");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_solve_status",
                table: "assigned_work",
                column: "solve_status");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_student_comment_id",
                table: "assigned_work",
                column: "student_comment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_student_id",
                table: "assigned_work",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_title",
                table: "assigned_work",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_work_id",
                table: "assigned_work",
                column: "work_id");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_answer_AssignedWorkModelId",
                table: "assigned_work_answer",
                column: "AssignedWorkModelId");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_answer_task_id",
                table: "assigned_work_answer",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_status_history_assigned_work_id",
                table: "assigned_work_status_history",
                column: "assigned_work_id");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_status_history_changed_by_id",
                table: "assigned_work_status_history",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_event_assigned_work_id",
                table: "calendar_event",
                column: "assigned_work_id");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_event_start_datetime",
                table: "calendar_event",
                column: "start_datetime");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_event_user_id",
                table: "calendar_event",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_name",
                table: "course",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_course_subject_id",
                table: "course",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_thumbnail_id",
                table: "course",
                column: "thumbnail_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_chapter_course_id",
                table: "course_chapter",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_chapter_parent_chapter_id",
                table: "course_chapter",
                column: "parent_chapter_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_material_chapter_id",
                table: "course_material",
                column: "chapter_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_material_content_id",
                table: "course_material",
                column: "content_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_course_material_content_work_id",
                table: "course_material_content",
                column: "work_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_membership_assigner_id",
                table: "course_membership",
                column: "assigner_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_membership_course_id",
                table: "course_membership",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_membership_student_id",
                table: "course_membership",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_mm_CoursesAsAuthor_user_course_id",
                table: "course_mm_CoursesAsAuthor_user",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_mm_CoursesAsEditor_user_user_id",
                table: "course_mm_CoursesAsEditor_user",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_reaction_material_id",
                table: "course_reaction",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_reaction_user_id",
                table: "course_reaction",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_media_hash",
                table: "media",
                column: "hash");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_assignment_mentor_id",
                table: "mentor_assignment",
                column: "mentor_id");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_assignment_student_id_mentor_id_subject_id",
                table: "mentor_assignment",
                columns: new[] { "student_id", "mentor_id", "subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mentor_assignment_subject_id",
                table: "mentor_assignment",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_nootube_video_thumbnail_id",
                table: "nootube_video",
                column: "thumbnail_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nootube_video_uploaded_by_id",
                table: "nootube_video",
                column: "uploaded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_nootube_video_comment_user_id",
                table: "nootube_video_comment",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_nootube_video_comment_video_id",
                table: "nootube_video_comment",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "IX_nootube_video_reaction_user_id_video_id",
                table: "nootube_video_reaction",
                columns: new[] { "user_id", "video_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nootube_video_reaction_video_id",
                table: "nootube_video_reaction",
                column: "video_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_user_id",
                table: "notification",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_poll_answer_poll_question_id",
                table: "poll_answer",
                column: "poll_question_id");

            migrationBuilder.CreateIndex(
                name: "IX_poll_answer_PollParticipationModelId",
                table: "poll_answer",
                column: "PollParticipationModelId");

            migrationBuilder.CreateIndex(
                name: "IX_poll_participation_poll_id",
                table: "poll_participation",
                column: "poll_id");

            migrationBuilder.CreateIndex(
                name: "IX_poll_participation_user_id",
                table: "poll_participation",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_poll_question_poll_id",
                table: "poll_question",
                column: "poll_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_user_id",
                table: "session",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_snippet_user_id",
                table: "snippet",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_support_article_category_id",
                table: "support_article",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_support_category_parent_id",
                table: "support_category",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_email",
                table: "user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_name",
                table: "user",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_user_telegram_id",
                table: "user",
                column: "telegram_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_telegram_username",
                table: "user",
                column: "telegram_username");

            migrationBuilder.CreateIndex(
                name: "IX_user_username",
                table: "user",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_avatar_media_id",
                table: "user_avatar",
                column: "media_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_avatar_user_id",
                table: "user_avatar",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_user_id",
                table: "user_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_subject_id",
                table: "work",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_title",
                table: "work",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "IX_work_type",
                table: "work",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_work_task_work_id",
                table: "work_task",
                column: "work_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assigned_work_answer");

            migrationBuilder.DropTable(
                name: "assigned_work_status_history");

            migrationBuilder.DropTable(
                name: "calendar_event");

            migrationBuilder.DropTable(
                name: "course_membership");

            migrationBuilder.DropTable(
                name: "course_mm_CoursesAsAuthor_user");

            migrationBuilder.DropTable(
                name: "course_mm_CoursesAsEditor_user");

            migrationBuilder.DropTable(
                name: "course_reaction");

            migrationBuilder.DropTable(
                name: "google_sheets_integration");

            migrationBuilder.DropTable(
                name: "mentor_assignment");

            migrationBuilder.DropTable(
                name: "nootube_video_comment");

            migrationBuilder.DropTable(
                name: "nootube_video_reaction");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "poll_answer");

            migrationBuilder.DropTable(
                name: "session");

            migrationBuilder.DropTable(
                name: "snippet");

            migrationBuilder.DropTable(
                name: "support_article");

            migrationBuilder.DropTable(
                name: "user_avatar");

            migrationBuilder.DropTable(
                name: "user_settings");

            migrationBuilder.DropTable(
                name: "work_task");

            migrationBuilder.DropTable(
                name: "assigned_work");

            migrationBuilder.DropTable(
                name: "course_material");

            migrationBuilder.DropTable(
                name: "nootube_video");

            migrationBuilder.DropTable(
                name: "poll_participation");

            migrationBuilder.DropTable(
                name: "poll_question");

            migrationBuilder.DropTable(
                name: "support_category");

            migrationBuilder.DropTable(
                name: "assigned_work_comment");

            migrationBuilder.DropTable(
                name: "course_chapter");

            migrationBuilder.DropTable(
                name: "course_material_content");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "poll");

            migrationBuilder.DropTable(
                name: "course");

            migrationBuilder.DropTable(
                name: "work");

            migrationBuilder.DropTable(
                name: "media");

            migrationBuilder.DropTable(
                name: "subject");
        }
    }
}
