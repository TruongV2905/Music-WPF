# 📚 Tài Liệu Chi Tiết: Playlist và Database

## 📋 Mục Lục

1. [Tổng Quan](#tổng-quan)
2. [Kiến Trúc Hệ Thống](#kiến-trúc-hệ-thống)
3. [Database SQLite](#database-sqlite)
4. [Kết Nối Database](#kết-nối-database)
5. [Cấu Trúc Bảng](#cấu-trúc-bảng)
6. [Các Thao Tác Database](#các-thao-tác-database)
7. [Luồng Hoạt Động](#luồng-hoạt-động)
8. [Code Examples](#code-examples)
9. [Best Practices](#best-practices)

---

## 🎯 Tổng Quan

### Chức Năng Playlist

Playlist cho phép người dùng:
- ✅ **Thêm** bài hát yêu thích vào danh sách
- ✅ **Xem** danh sách bài hát đã lưu
- ✅ **Phát** bài hát từ playlist
- ✅ **Xóa** bài hát khỏi playlist
- ✅ **Lưu trữ vĩnh viễn** trong database

### Database

- **Loại**: SQLite (file-based database)
- **Vị trí**: `[ProjectRoot]/Data/playlist.db`
- **Cách tiếp cận**: DB-First (tạo bảng bằng SQL thuần)
- **Thư viện**: `Microsoft.Data.Sqlite` (Version 9.0.10)

---

## 🏗️ Kiến Trúc Hệ Thống

### Cấu Trúc Thư Mục

```
Music-WPF/
├── Data/                              ← Database được lưu ở đây
│   └── playlist.db                   ← File SQLite database
│
├── Group1.MusicApp/                  ← WPF Application
│   ├── Views/
│   │   └── PlaylistView.xaml         ← Giao diện playlist
│   │   └── PlaylistView.xaml.cs      ← Code-behind
│   └── ViewModels/
│       └── PlaylistViewModel.cs      ← ViewModel quản lý dữ liệu
│
├── Group1.ApiClient/                  ← API Client & Services
│   └── Services/
│       └── PlaylistService.cs        ← Service xử lý database
│
└── Group1.MusicApp.Models/            ← Models
    └── PlaylistItem.cs                ← Model cho playlist item
```

### Kiến Trúc 3 Lớp (3-Tier Architecture)

```
┌─────────────────────────────────────┐
│         VIEW LAYER                   │
│  (PlaylistView.xaml + .xaml.cs)     │
│  - Hiển thị UI                       │
│  - Xử lý user interaction            │
└──────────────┬───────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│      VIEWMODEL LAYER                │
│  (PlaylistViewModel.cs)             │
│  - Quản lý dữ liệu                  │
│  - Gọi Service                      │
│  - Business logic đơn giản          │
└──────────────┬───────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│       SERVICE LAYER                 │
│  (PlaylistService.cs)               │
│  - Kết nối database                 │
│  - CRUD operations                   │
│  - SQL queries                      │
└──────────────┬───────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│        DATABASE LAYER                │
│  (SQLite - playlist.db)             │
│  - Lưu trữ dữ liệu                  │
│  - Bảng PlaylistItems               │
└─────────────────────────────────────┘
```

---

## 🗄️ Database SQLite

### Tại Sao Chọn SQLite?

1. ✅ **Không cần server**: File-based, không cần cài đặt SQL Server
2. ✅ **Nhẹ**: Database chỉ là một file `.db`
3. ✅ **Dễ backup**: Copy file là xong
4. ✅ **Tự động tạo**: Tự động tạo file nếu chưa có
5. ✅ **Phù hợp desktop app**: Lý tưởng cho ứng dụng WPF

### Vị Trí Database

**Đường dẫn đầy đủ:**
```
E:\FPTU\Semester-5\PRN212\Assignment\Music-WPF\Data\playlist.db
```

**Cách xác định đường dẫn:**
```csharp
// 1. Lấy thư mục chứa file .exe
string exePath = AppDomain.CurrentDomain.BaseDirectory;
// → bin/Debug/net9.0-windows/

// 2. Đi lên 4 cấp để đến project root
string projectRoot = Path.GetFullPath(Path.Combine(exePath, "..", "..", "..", ".."));
// → Music-WPF/

// 3. Tạo thư mục Data
string dataFolder = Path.Combine(projectRoot, "Data");
// → Music-WPF/Data/

// 4. Tạo đường dẫn file database
dbPath = Path.Combine(dataFolder, "playlist.db");
// → Music-WPF/Data/playlist.db
```

---

## 🔌 Kết Nối Database

### Connection String

```csharp
connectionString = "Data Source=" + dbPath;
```

**Ví dụ:**
```
Data Source=E:\FPTU\Semester-5\PRN212\Assignment\Music-WPF\Data\playlist.db
```

### Pattern Kết Nối (Mở → Thực Thi → Đóng)

Mỗi thao tác database đều theo pattern:

```csharp
// 1. Tạo connection
SqliteConnection connection = new SqliteConnection(connectionString);

// 2. Mở connection
connection.Open();

// 3. Tạo command và thực thi
SqliteCommand command = connection.CreateCommand();
command.CommandText = "SQL QUERY HERE";
command.ExecuteNonQuery(); // hoặc ExecuteReader(), ExecuteScalar()

// 4. Đóng connection
connection.Close();
```

### Tự Động Tạo Database

Database và bảng được tạo tự động khi chạy app lần đầu:

```csharp
public PlaylistService()
{
    // ... Tạo đường dẫn ...
    
    // Tự động gọi InitializeDatabase()
    InitializeDatabase();
}

private void InitializeDatabase()
{
    SqliteConnection connection = new SqliteConnection(connectionString);
    connection.Open();
    
    SqliteCommand command = connection.CreateCommand();
    command.CommandText = @"
        CREATE TABLE IF NOT EXISTS PlaylistItems (
            ...
        );
    ";
    
    command.ExecuteNonQuery();
    connection.Close();
}
```

**Lưu ý:**
- `CREATE TABLE IF NOT EXISTS` → Chỉ tạo nếu chưa có
- Nếu file `.db` chưa tồn tại → SQLite tự động tạo file mới
- Nếu bảng đã có → Không làm gì cả

---

## 📊 Cấu Trúc Bảng

### Bảng: `PlaylistItems`

```sql
CREATE TABLE IF NOT EXISTS PlaylistItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TrackId TEXT NOT NULL UNIQUE,
    TrackName TEXT NOT NULL,
    ArtistName TEXT NOT NULL,
    AlbumName TEXT,
    AlbumImageUrl TEXT,
    DurationMs INTEGER NOT NULL,
    PreviewUrl TEXT,
    AddedDate TEXT NOT NULL
);
```

### Chi Tiết Các Cột

| Cột | Kiểu Dữ Liệu | Ràng Buộc | Mô Tả |
|-----|--------------|-----------|-------|
| `Id` | INTEGER | PRIMARY KEY, AUTOINCREMENT | ID tự tăng, khóa chính |
| `TrackId` | TEXT | NOT NULL, UNIQUE | ID bài hát (không trùng lặp) |
| `TrackName` | TEXT | NOT NULL | Tên bài hát |
| `ArtistName` | TEXT | NOT NULL | Tên ca sĩ |
| `AlbumName` | TEXT | NULL | Tên album (có thể null) |
| `AlbumImageUrl` | TEXT | NULL | URL ảnh album (có thể null) |
| `DurationMs` | INTEGER | NOT NULL | Thời lượng (milliseconds) |
| `PreviewUrl` | TEXT | NULL | URL preview audio (có thể null) |
| `AddedDate` | TEXT | NOT NULL | Ngày thêm (format: yyyy-MM-dd HH:mm:ss) |

### Ràng Buộc (Constraints)

1. **PRIMARY KEY**: `Id` là khóa chính, tự động tăng
2. **UNIQUE**: `TrackId` không được trùng lặp
3. **NOT NULL**: Các trường bắt buộc phải có giá trị
4. **NULL**: Các trường có thể để trống

---

## 🔧 Các Thao Tác Database

### 1. CREATE - Thêm Bài Hát

**Method:** `AddTrack(Track track)`

**SQL Query:**
```sql
INSERT OR IGNORE INTO PlaylistItems 
(TrackId, TrackName, ArtistName, AlbumName, AlbumImageUrl, DurationMs, PreviewUrl, AddedDate)
VALUES (@TrackId, @TrackName, @ArtistName, @AlbumName, @AlbumImageUrl, @DurationMs, @PreviewUrl, @AddedDate)
```

**Code:**
```csharp
public bool AddTrack(Track track)
{
    try
    {
        SqliteConnection connection = new SqliteConnection(connectionString);
        connection.Open();
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"INSERT OR IGNORE INTO PlaylistItems ...";
        
        // Thêm parameters (tránh SQL injection)
        command.Parameters.AddWithValue("@TrackId", track.Id);
        command.Parameters.AddWithValue("@TrackName", track.Name);
        // ... các parameters khác ...
        command.Parameters.AddWithValue("@AddedDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        
        int rowsAffected = command.ExecuteNonQuery();
        connection.Close();
        
        return rowsAffected > 0; // true nếu thêm thành công
    }
    catch (Exception ex)
    {
        throw new Exception("Lỗi khi thêm bài hát: " + ex.Message, ex);
    }
}
```

**Đặc điểm:**
- ✅ `INSERT OR IGNORE` → Nếu TrackId đã có, bỏ qua (không báo lỗi)
- ✅ Sử dụng **Parameterized Query** → Tránh SQL injection
- ✅ Tự động thêm `AddedDate` = thời gian hiện tại

### 2. READ - Lấy Danh Sách Bài Hát

**Method:** `GetAllTracks()`

**SQL Query:**
```sql
SELECT Id, TrackId, TrackName, ArtistName, AlbumName, AlbumImageUrl, DurationMs, PreviewUrl, AddedDate
FROM PlaylistItems
ORDER BY AddedDate DESC
```

**Code:**
```csharp
public List<PlaylistItem> GetAllTracks()
{
    List<PlaylistItem> tracks = new List<PlaylistItem>();
    
    try
    {
        SqliteConnection connection = new SqliteConnection(connectionString);
        connection.Open();
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"SELECT ... FROM PlaylistItems ORDER BY AddedDate DESC";
        
        SqliteDataReader reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            PlaylistItem item = new PlaylistItem();
            item.Id = reader.GetInt32(0);
            item.TrackId = reader.GetString(1);
            item.TrackName = reader.GetString(2);
            // ... đọc các cột khác ...
            
            // Kiểm tra null trước khi đọc
            if (!reader.IsDBNull(4))
                item.AlbumName = reader.GetString(4);
            
            tracks.Add(item);
        }
        
        reader.Close();
        connection.Close();
    }
    catch (Exception ex)
    {
        throw new Exception("Lỗi khi lấy danh sách: " + ex.Message, ex);
    }
    
    return tracks;
}
```

**Đặc điểm:**
- ✅ Sử dụng `SqliteDataReader` để đọc từng dòng
- ✅ Kiểm tra `IsDBNull()` trước khi đọc giá trị nullable
- ✅ Sắp xếp theo `AddedDate DESC` → Bài mới nhất ở trên
- ✅ Convert từ database row → `PlaylistItem` object

### 3. DELETE - Xóa Bài Hát

**Method:** `RemoveTrack(string trackId)`

**SQL Query:**
```sql
DELETE FROM PlaylistItems WHERE TrackId = @TrackId
```

**Code:**
```csharp
public bool RemoveTrack(string trackId)
{
    try
    {
        SqliteConnection connection = new SqliteConnection(connectionString);
        connection.Open();
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM PlaylistItems WHERE TrackId = @TrackId";
        command.Parameters.AddWithValue("@TrackId", trackId);
        
        int rowsAffected = command.ExecuteNonQuery();
        connection.Close();
        
        return rowsAffected > 0; // true nếu xóa thành công
    }
    catch (Exception ex)
    {
        throw new Exception("Lỗi khi xóa bài hát: " + ex.Message, ex);
    }
}
```

**Đặc điểm:**
- ✅ Xóa theo `TrackId` (không phải `Id`)
- ✅ Sử dụng parameter để tránh SQL injection
- ✅ Trả về `true` nếu xóa thành công

### 4. CHECK - Kiểm Tra Bài Hát Có Trong Playlist

**Method:** `IsTrackInPlaylist(string trackId)`

**SQL Query:**
```sql
SELECT COUNT(*) FROM PlaylistItems WHERE TrackId = @TrackId
```

**Code:**
```csharp
public bool IsTrackInPlaylist(string trackId)
{
    try
    {
        SqliteConnection connection = new SqliteConnection(connectionString);
        connection.Open();
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM PlaylistItems WHERE TrackId = @TrackId";
        command.Parameters.AddWithValue("@TrackId", trackId);
        
        object? result = command.ExecuteScalar();
        int count = result != null ? Convert.ToInt32(result) : 0;
        
        connection.Close();
        
        return count > 0; // true nếu có bài hát
    }
    catch
    {
        return false;
    }
}
```

**Đặc điểm:**
- ✅ Sử dụng `ExecuteScalar()` để lấy một giá trị duy nhất
- ✅ `COUNT(*)` trả về số lượng dòng
- ✅ Nếu `count > 0` → Bài hát đã có trong playlist

### 5. COUNT - Đếm Số Lượng Bài Hát

**Method:** `GetTrackCount()`

**SQL Query:**
```sql
SELECT COUNT(*) FROM PlaylistItems
```

**Code:**
```csharp
public int GetTrackCount()
{
    try
    {
        SqliteConnection connection = new SqliteConnection(connectionString);
        connection.Open();
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM PlaylistItems";
        
        object? result = command.ExecuteScalar();
        int count = result != null ? Convert.ToInt32(result) : 0;
        
        connection.Close();
        
        return count;
    }
    catch
    {
        return 0;
    }
}
```

---

## 🔄 Luồng Hoạt Động

### Luồng Thêm Bài Hát

```
1. User click nút "+" ở bài hát
   ↓
2. MainWindow.btnAddToPlaylist_Click()
   ↓
3. PlaylistViewControl.AddTrack(track)
   ↓
4. PlaylistViewModel.AddTrack(track)
   ↓
5. PlaylistService.IsTrackInPlaylist(trackId)
   ↓
6. [Database] SELECT COUNT(*) WHERE TrackId = ...
   ↓
7. Nếu chưa có → PlaylistService.AddTrack(track)
   ↓
8. [Database] INSERT INTO PlaylistItems ...
   ↓
9. PlaylistViewModel.LoadPlaylist()
   ↓
10. PlaylistService.GetAllTracks()
   ↓
11. [Database] SELECT * FROM PlaylistItems ...
   ↓
12. UI Refresh → Hiển thị bài hát mới
```

### Luồng Xóa Bài Hát

```
1. User click nút Delete (🗑️)
   ↓
2. PlaylistView.DeleteButton_Click()
   ↓
3. MessageBox xác nhận
   ↓
4. Nếu Yes → PlaylistViewModel.RemoveTrack(trackId)
   ↓
5. PlaylistService.RemoveTrack(trackId)
   ↓
6. [Database] DELETE FROM PlaylistItems WHERE TrackId = ...
   ↓
7. UI Refresh → Xóa bài hát khỏi danh sách
```

### Luồng Load Playlist

```
1. App khởi động
   ↓
2. MainWindow.Window_Loaded()
   ↓
3. PlaylistViewControl.Refresh()
   ↓
4. PlaylistViewModel.LoadPlaylist()
   ↓
5. PlaylistService.GetAllTracks()
   ↓
6. [Database] SELECT * FROM PlaylistItems ORDER BY AddedDate DESC
   ↓
7. Convert database rows → PlaylistItem objects
   ↓
8. UI hiển thị danh sách bài hát
```

---

## 💻 Code Examples

### Example 1: Thêm Bài Hát Vào Playlist

```csharp
// Trong MainWindow.xaml.cs
private void btnAddToPlaylist_Click(object sender, RoutedEventArgs e)
{
    Button? button = sender as Button;
    if (button == null) return;

    Track? track = button.Tag as Track;
    if (track == null) return;

    try
    {
        // Kiểm tra đã có chưa
        if (PlaylistViewControl.IsTrackInPlaylist(track.Id))
        {
            MessageBox.Show("Bài hát này đã có trong playlist!");
            return;
        }

        // Thêm vào playlist
        bool success = PlaylistViewControl.AddTrack(track);
        if (success)
        {
            MessageBox.Show($"Đã thêm '{track.Name}' vào playlist!");
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Lỗi: {ex.Message}");
    }
}
```

### Example 2: Xóa Bài Hát

```csharp
// Trong PlaylistView.xaml.cs
private void DeleteButton_Click(object sender, RoutedEventArgs e)
{
    Button? button = sender as Button;
    if (button == null) return;

    string? trackId = button.Tag?.ToString();
    if (string.IsNullOrEmpty(trackId)) return;

    // Xác nhận xóa
    MessageBoxResult result = MessageBox.Show(
        "Bạn có chắc muốn xóa bài hát này?",
        "Xóa bài hát",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

    if (result == MessageBoxResult.Yes)
    {
        // Xóa khỏi database
        viewModel.RemoveTrack(trackId);
        
        // Refresh UI
        PlaylistItemsControl.ItemsSource = null;
        PlaylistItemsControl.ItemsSource = viewModel.PlaylistItems;
        
        UpdateTrackCount();
        UpdateEmptyState();
    }
}
```

### Example 3: Đọc Dữ Liệu Từ Database

```csharp
// Trong PlaylistService.cs
public List<PlaylistItem> GetAllTracks()
{
    List<PlaylistItem> tracks = new List<PlaylistItem>();

    SqliteConnection connection = new SqliteConnection(connectionString);
    connection.Open();

    SqliteCommand command = connection.CreateCommand();
    command.CommandText = @"
        SELECT Id, TrackId, TrackName, ArtistName, AlbumName, 
               AlbumImageUrl, DurationMs, PreviewUrl, AddedDate
        FROM PlaylistItems
        ORDER BY AddedDate DESC
    ";

    SqliteDataReader reader = command.ExecuteReader();
    
    while (reader.Read())
    {
        PlaylistItem item = new PlaylistItem();
        
        // Đọc từng cột (index bắt đầu từ 0)
        item.Id = reader.GetInt32(0);
        item.TrackId = reader.GetString(1);
        item.TrackName = reader.GetString(2);
        item.ArtistName = reader.GetString(3);
        
        // Kiểm tra null cho các cột có thể null
        if (!reader.IsDBNull(4))
            item.AlbumName = reader.GetString(4);
        if (!reader.IsDBNull(5))
            item.AlbumImageUrl = reader.GetString(5);
        
        item.DurationMs = reader.GetInt32(6);
        
        if (!reader.IsDBNull(7))
            item.PreviewUrl = reader.GetString(7);
        
        item.AddedDate = DateTime.Parse(reader.GetString(8));
        
        tracks.Add(item);
    }

    reader.Close();
    connection.Close();

    return tracks;
}
```

---

## 🛡️ Best Practices

### 1. Parameterized Queries (Tránh SQL Injection)

**❌ KHÔNG NÊN:**
```csharp
command.CommandText = $"DELETE FROM PlaylistItems WHERE TrackId = '{trackId}'";
```

**✅ NÊN:**
```csharp
command.CommandText = "DELETE FROM PlaylistItems WHERE TrackId = @TrackId";
command.Parameters.AddWithValue("@TrackId", trackId);
```

### 2. Luôn Đóng Connection

**✅ Pattern đúng:**
```csharp
SqliteConnection connection = new SqliteConnection(connectionString);
connection.Open();
try
{
    // ... thao tác database ...
}
finally
{
    connection.Close(); // Luôn đóng connection
}
```

### 3. Kiểm Tra Null Trước Khi Đọc

**✅ Đúng:**
```csharp
if (!reader.IsDBNull(4))
    item.AlbumName = reader.GetString(4);
```

**❌ Sai:**
```csharp
item.AlbumName = reader.GetString(4); // Có thể lỗi nếu null
```

### 4. Xử Lý Exception

**✅ Có xử lý lỗi:**
```csharp
try
{
    // ... thao tác database ...
}
catch (Exception ex)
{
    throw new Exception("Lỗi khi thêm bài hát: " + ex.Message, ex);
}
```

### 5. Sử Dụng `INSERT OR IGNORE`

**✅ Tránh lỗi duplicate:**
```sql
INSERT OR IGNORE INTO PlaylistItems ...
```

Thay vì:
```sql
INSERT INTO PlaylistItems ... -- Có thể lỗi nếu TrackId trùng
```

### 6. Sắp Xếp Kết Quả

**✅ Sắp xếp theo ngày thêm:**
```sql
SELECT * FROM PlaylistItems ORDER BY AddedDate DESC
```

→ Bài hát mới nhất hiển thị đầu tiên

---

## 📦 Models

### PlaylistItem Model

```csharp
public class PlaylistItem
{
    // Properties từ database
    public int Id { get; set; }
    public string TrackId { get; set; }
    public string TrackName { get; set; }
    public string ArtistName { get; set; }
    public string AlbumName { get; set; }
    public string AlbumImageUrl { get; set; }
    public int DurationMs { get; set; }
    public string PreviewUrl { get; set; }
    public DateTime AddedDate { get; set; }

    // Computed properties (tính toán từ properties khác)
    public string Duration => TimeSpan.FromMilliseconds(DurationMs).ToString(@"m\:ss");
    // Ví dụ: 180000 ms → "3:00"
    
    public string AddedDateText => AddedDate.ToString("MMM dd, yyyy");
    // Ví dụ: "Jan 15, 2024"
}
```

**Computed Properties:**
- Không lưu trong database
- Tính toán từ properties khác
- Dùng để hiển thị trong UI

---

## 🔍 Debugging Database

### Xem Nội Dung Database

**Cách 1: Dùng DB Browser for SQLite**
1. Download: https://sqlitebrowser.org/
2. Mở file `Data/playlist.db`
3. Xem bảng `PlaylistItems`

**Cách 2: Dùng SQLite Command Line**
```bash
sqlite3 Data/playlist.db
.tables                    # Xem danh sách bảng
SELECT * FROM PlaylistItems;  # Xem tất cả dữ liệu
```

### Kiểm Tra Database Có Tạo Thành Công

```csharp
// Thêm vào PlaylistService constructor để debug
public PlaylistService()
{
    // ... code tạo đường dẫn ...
    
    Console.WriteLine($"Database path: {dbPath}");
    Console.WriteLine($"Database exists: {File.Exists(dbPath)}");
    
    InitializeDatabase();
}
```

### Kiểm Tra Số Lượng Bài Hát

```csharp
// Trong PlaylistService
public int GetTrackCount()
{
    // ... code đếm ...
    Console.WriteLine($"Total tracks: {count}");
    return count;
}
```

---

## ⚠️ Lưu Ý Quan Trọng

### 1. Database Location

- **Trước đây**: `%LocalAppData%\MusicApp\playlist.db`
- **Bây giờ**: `[ProjectRoot]/Data/playlist.db`

Nếu bạn đã có database cũ, có thể:
- Copy file từ AppData sang `Data/`
- Hoặc để app tạo database mới

### 2. Git Ignore

File `.gitignore` đã được cập nhật để không commit database:
```
Data/
playlist.db
*.db
```

### 3. Backup Database

Để backup:
```bash
# Copy file database
copy Data\playlist.db Data\playlist.db.backup
```

### 4. Xóa Database

Để reset playlist:
```bash
# Xóa file database
del Data\playlist.db
```

App sẽ tự động tạo lại khi chạy.

---

## 📚 Tài Liệu Tham Khảo

### SQLite Documentation
- Official: https://www.sqlite.org/docs.html
- .NET: https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/

### Microsoft.Data.Sqlite
- NuGet: https://www.nuget.org/packages/Microsoft.Data.Sqlite
- API Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlite

### SQL Syntax
- SQLite SQL: https://www.sqlite.org/lang.html
- CREATE TABLE: https://www.sqlite.org/lang_createtable.html
- INSERT: https://www.sqlite.org/lang_insert.html
- SELECT: https://www.sqlite.org/lang_select.html
- DELETE: https://www.sqlite.org/lang_delete.html

---

## 🎓 Kiến Thức Áp Dụng

### 1. ADO.NET
- `SqliteConnection` - Kết nối database
- `SqliteCommand` - Thực thi SQL
- `SqliteDataReader` - Đọc dữ liệu
- `ExecuteNonQuery()` - INSERT, UPDATE, DELETE
- `ExecuteReader()` - SELECT nhiều dòng
- `ExecuteScalar()` - SELECT một giá trị

### 2. SQL
- `CREATE TABLE` - Tạo bảng
- `INSERT` - Thêm dữ liệu
- `SELECT` - Đọc dữ liệu
- `DELETE` - Xóa dữ liệu
- `COUNT()` - Đếm số lượng
- `ORDER BY` - Sắp xếp
- `WHERE` - Điều kiện

### 3. C# Patterns
- **DB-First**: Tạo bảng bằng SQL, không dùng EF
- **Repository Pattern**: Service layer tách biệt database logic
- **MVVM**: ViewModel tách biệt View và Model
- **Parameterized Queries**: Tránh SQL injection

---

## ✅ Tổng Kết

### Những Gì Đã Triển Khai

1. ✅ **Database SQLite** - File-based, không cần server
2. ✅ **Tự động tạo** - Database và bảng tự động tạo khi chạy
3. ✅ **CRUD đầy đủ** - Create, Read, Delete, Check, Count
4. ✅ **Parameterized Queries** - An toàn, tránh SQL injection
5. ✅ **Error Handling** - Xử lý lỗi đầy đủ
6. ✅ **DB-First Approach** - SQL thuần, dễ hiểu
7. ✅ **Persistent Storage** - Dữ liệu lưu vĩnh viễn

### Code Đơn Giản, Dễ Hiểu

- ✅ Không dùng Entity Framework
- ✅ Không dùng LINQ phức tạp
- ✅ SQL thuần, rõ ràng
- ✅ Vòng lặp for/foreach đơn giản
- ✅ If/else logic rõ ràng

---

**Tài liệu này giải thích chi tiết cách Playlist và Database hoạt động trong ứng dụng Music WPF của bạn!** 🎵✨

