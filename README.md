# GameHub

אתר משחקים לשני שחקנים. שחקן פותח משחק, מקבל קוד קצר (למשל `K7M2Q`) ושולח אותו לחבר. החבר מקליד את הקוד, והמשחק מתחיל. שלושה משחקים: **איסוף שלל**, **מרוץ מספרים** ו**חידון מהיר**. בכולם אין תורות: שני השחקנים פועלים באותו זמן, ומי שהספיק ראשון מקבל את הנקודות. הפרויקט הזה הוא השרת (ASP.NET Core Web API, .NET 8, PostgreSQL). הלקוח (React) נמצא ב-repository נפרד.

## המשאב המוגבל והטיפול בתחרות

יש שתי נקודות תחרות:

1. **המקום השני בחדר** (`GameSession`). שני שחקנים שמקלידים את אותו קוד באותו רגע מתחרים על מקום האורח.
2. **פריט משחק** (`GameItem`): תיבה, מספר או שאלה. שני שחקנים שלוחצים על אותו פריט באותו רגע מתחרים על התפיסה שלו.

בשתי הישויות יש concurrency token (`Guid Version`, מוגדר ב-Fluent API, ומוחלף בכל עדכון בדריסה של `SaveChangesAsync` ב-`AppDbContext`). כששני שחקנים קוראים את אותה שורה כפנויה ושניהם מנסים לשמור, רק הראשון מצליח. בשני נזרקת `DbUpdateConcurrencyException`. ה-Repository (בשכבת Data) ממיר אותה ל-`ConcurrencyConflictException` שמוגדרת ב-Core, כך ששכבת ה-Service לא תלויה ב-EF Core. ה-Service מחזיר `Result.Conflict`, ה-API מחזיר **409** עם הודעה ברורה, ונרשם לוג ברמת **Warning**.

אם מישהו כבר הצטרף ושמר לפני שהשני קרא את החדר, זו לא תחרות אלא חדר מלא רגיל. הוא נתפס בבדיקה רגילה, ומחזיר גם הוא 409.

הניקוד לא מתעדכן בכל תפיסה (זה היה יוצר התנגשויות מזויפות). הוא מחושב מסכום הנקודות של הפריטים שנתפסו, ונכתב פעם אחת בסיום המשחק. המשחק נסגר "עצלנית": הבקשה הבאה אחרי שהזמן נגמר מזהה זאת וסוגרת אותו, בלי טיימר ובלי תהליך רקע.

## ארכיטקטורה

```
GameHub.Core      // Entities, Interfaces, DTOs, Enums, Results, Exceptions (בלי חבילות חיצוניות)
GameHub.Data      // AppDbContext, Repositories, Migrations (EF Core + Npgsql)
GameHub.Service   // לוגיקה עסקית, AutoMapper, BCrypt
GameHub.API       // Controllers, Middleware, Program.cs
GameHub.Tests     // xUnit + Moq, ובדיקות אינטגרציה מול PostgreSQL
```

כיוון התלויות: Core לא מפנה לאף פרויקט. Data ו-Service מפנים ל-Core. ה-API מפנה ל-Service ול-Core, ומכיר את Data רק לצורך רישום שירותים ב-`Program.cs`. Controller לא מזריק `DbContext`.

## דרישות מוקדמות

- .NET 8 SDK
- Docker Desktop (או התקנה ישירה של PostgreSQL 16)
- הכלי `dotnet-ef`: `dotnet tool install --global dotnet-ef`

## הרצה מקומית (Windows, CMD)

כל הפקודות מתיקיית השורש של ה-solution.

**1. בסיס נתונים (PostgreSQL ב-Docker):**

```
docker run --name gamehub-pg -e POSTGRES_PASSWORD=<DB_PASSWORD> -p 5432:5432 -d postgres:16
```

אין צורך ליצור את בסיס הנתונים `gamehub` ידנית. הוא נוצר בהרצה הראשונה.

**2. סודות (User Secrets).** אף סוד לא נמצא בקוד או ב-repo. שבע פקודות, פעם אחת:

```
dotnet user-secrets init --project GameHub.API
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=gamehub;Username=postgres;Password=<DB_PASSWORD>" --project GameHub.API
dotnet user-secrets set "Jwt:Key" "<מחרוזת אקראית של 32 תווים לפחות>" --project GameHub.API
dotnet user-secrets set "Seed:AdminUsername" "admin" --project GameHub.API
dotnet user-secrets set "Seed:AdminPassword" "<ADMIN_PASSWORD>" --project GameHub.API
dotnet user-secrets set "Seed:DemoUsername" "demo" --project GameHub.API
dotnet user-secrets set "Seed:DemoPassword" "<DEMO_PASSWORD>" --project GameHub.API
```

סיסמאות הדמו חייבות להיות באורך 8 תווים לפחות.

**3. הרצת השרת:**

```
dotnet run --project GameHub.API --launch-profile http
```

השרת עולה ב-`http://localhost:5163`, ו-Swagger ב-`http://localhost:5163/swagger`. בהרצה בסביבת `Development` מתבצעים אוטומטית `Database.MigrateAsync()` ו-Seed (מנהל, שחקן דמו ושלוש שורות `GameSetting`), בלי התערבות ידנית בבסיס הנתונים. בסביבה אחרת הם לא רצים.

**Migrations ידנית (אופציונלי):**

```
dotnet ef migrations list -p GameHub.Data -s GameHub.API
dotnet ef database update -p GameHub.Data -s GameHub.API
```

בהיסטוריה ארבע migrations: `Init`, `AddItemSlot`, `AddDisplayMs`, `UsernameCaseInsensitive`.

## משתמשי דמו

| Role | שם משתמש | סיסמה |
|---|---|---|
| Admin | `admin` | `<ADMIN_PASSWORD>` |
| Player | `demo` | `<DEMO_PASSWORD>` |

הערכים הם אלה שהוגדרו ב-`Seed:*` בשלב 2. חשבונות נוספים אפשר ליצור דרך `POST /api/auth/register` (נוצר תמיד Role של Player).

## Endpoints עיקריים

| Method | Path | הרשאה |
|---|---|---|
| POST | `/api/auth/register`, `/api/auth/login` | פתוח |
| POST | `/api/game-sessions` | מחובר |
| POST | `/api/game-sessions/{code}/players` | מחובר |
| GET | `/api/game-sessions/{id}` | משתתף בלבד |
| POST | `/api/game-sessions/{id}/captures` | משתתף בלבד |
| GET | `/api/game-history` | מחובר (מנהל רואה את כולם) |
| GET / PUT | `/api/game-settings`, `/api/game-settings/{gameType}` | GET: מחובר, PUT: Admin |
| GET | `/api/admin/game-sessions` | Admin |

בקשה בלי token תקף מקבלת 401, ובקשה בלי הרשאה מתאימה מקבלת 403. שגיאות מוחזרות כ-JSON אחיד: `{ status, message, correlationId }`.

## בדיקות

```
dotnet test
```

בדיקות Service (xUnit + Moq) לא דורשות בסיס נתונים. בדיקות האינטגרציה, כולל הבדיקה שמוכיחה את ה-concurrency (שני `DbContext` על אותה ישות: הראשון שומר, השני מקבל `DbUpdateConcurrencyException`), רצות מול PostgreSQL אמיתי ודורשות משתנה סביבה לפני ההרצה:

```
set GAMEHUB_TEST_DB=Host=localhost;Port=5432;Username=postgres;Password=<DB_PASSWORD>;Database=postgres
```

כל בדיקה יוצרת בסיס נתונים זמני משלה ומוחקת אותו.

## לוגים

NLog (`NLog.Web.AspNetCore`), הגדרות ב-`nlog.config`. הלוג נכתב לקונסול ולקובץ `GameHub.API\bin\Debug\net8.0\logs\gamehub-<תאריך>.log`. נרשמים: כל בקשה נכנסת עם `CorrelationId`, כל חריגה (Error), וכל התנגשות concurrency (Warning). סיסמאות, tokens וגופי בקשות הרשמה והתחברות לא נרשמים.

## הערות

- בנייה מציגה אזהרת NU1903 על AutoMapper 12.0.1 (פגיעות ידועה). לפני שדרוג לגרסה חדשה יש לבדוק את תנאי הרישוי שלה.
- אין pagination ב-`/api/game-history` וב-`/api/admin/game-sessions`. תקרה קשיחה של 200 שורות, לפי עדכון המורה.
