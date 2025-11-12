# Hướng Dẫn Sử Dụng Playlist

## Tổng Quan
Chức năng Playlist cho phép bạn lưu các bài hát yêu thích và quản lý chúng dễ dàng.

## Các Tính Năng Chính

### 1. Thêm Bài Hát Vào Playlist
- Khi bạn tìm kiếm bài hát, mỗi bài hát sẽ có nút **"+"** màu xanh ở bên phải
- Click vào nút **"+"** để thêm bài hát vào playlist
- Nếu bài hát đã có trong playlist, hệ thống sẽ thông báo
- Nếu thêm thành công, bạn sẽ thấy thông báo xác nhận

### 2. Xem Playlist
- Click vào **"📚 Your Library"** ở thanh bên trái
- Hoặc click vào **"❤️ My Playlist"** 
- Bạn sẽ thấy danh sách tất cả bài hát đã lưu
- Mỗi bài hát hiển thị:
  - Ảnh album
  - Tên bài hát
  - Tên ca sĩ
  - Ngày thêm vào
  - Thời lượng

### 3. Phát Bài Hát Từ Playlist
- Trong playlist, mỗi bài hát có nút **Play** (▶) màu xanh
- Click vào nút **Play** để phát bài hát
- Bài hát sẽ được phát ngay lập tức

### 4. Xóa Bài Hát Khỏi Playlist
- Trong playlist, mỗi bài hát có nút **Delete** (🗑️) màu đỏ
- Click vào nút **Delete** để xóa bài hát
- Hệ thống sẽ hỏi xác nhận trước khi xóa
- Click **Yes** để xác nhận xóa

## Lưu Trữ Dữ Liệu

### Database
- Playlist được lưu trong file database SQLite
- Đường dẫn: `%LocalAppData%\MusicApp\playlist.db`
- Database tự động được tạo khi chạy app lần đầu
- Dữ liệu được lưu vĩnh viễn, không bị mất khi đóng app

### Bảng PlaylistItems
Cấu trúc bảng lưu trữ playlist:

| Cột            | Kiểu dữ liệu | Mô tả                          |
|----------------|--------------|--------------------------------|
| Id             | INTEGER      | Khóa chính, tự động tăng       |
| TrackId        | TEXT         | ID bài hát (unique)            |
| TrackName      | TEXT         | Tên bài hát                    |
| ArtistName     | TEXT         | Tên ca sĩ                      |
| AlbumName      | TEXT         | Tên album                      |
| AlbumImageUrl  | TEXT         | URL ảnh album                  |
| DurationMs     | INTEGER      | Thời lượng (milliseconds)      |
| PreviewUrl     | TEXT         | URL để phát nhạc               |
| AddedDate      | TEXT         | Ngày thêm vào playlist         |

## Luồng Hoạt Động

### Khi Khởi Động App
1. App kiểm tra file database có tồn tại không
2. Nếu chưa có, tạo file database mới
3. Tạo bảng `PlaylistItems` nếu chưa có
4. Tải danh sách bài hát từ database lên playlist

### Khi Thêm Bài Hát
1. User click nút "+" ở bài hát
2. Kiểm tra bài hát đã có trong playlist chưa
3. Nếu chưa có, thêm vào database
4. Cập nhật giao diện playlist
5. Hiển thị thông báo thành công

### Khi Xóa Bài Hát
1. User click nút Delete
2. Hiển thị hộp thoại xác nhận
3. Nếu user chọn Yes, xóa khỏi database
4. Cập nhật giao diện playlist

### Khi Phát Bài Hát
1. User click nút Play trong playlist
2. Lấy thông tin bài hát từ playlist
3. Gọi API để lấy thông tin chi tiết
4. Phát bài hát và hiển thị lời bài hát

## Cấu Trúc Code

### Models
- **PlaylistItem.cs**: Model chứa thông tin bài hát trong playlist

### Services
- **PlaylistService.cs**: Xử lý tất cả các thao tác với database
  - `AddTrack()`: Thêm bài hát
  - `RemoveTrack()`: Xóa bài hát
  - `GetAllTracks()`: Lấy tất cả bài hát
  - `IsTrackInPlaylist()`: Kiểm tra bài hát có trong playlist không
  - `GetTrackCount()`: Đếm số lượng bài hát

### ViewModels
- **PlaylistViewModel.cs**: Quản lý dữ liệu cho PlaylistView
  - Gọi PlaylistService để thao tác với database
  - Quản lý danh sách bài hát hiển thị

### Views
- **PlaylistView.xaml**: Giao diện hiển thị playlist
- **PlaylistView.xaml.cs**: Code xử lý sự kiện của PlaylistView

### MainWindow
- **MainWindow.xaml**: Thêm nút "+" cho mỗi bài hát trong kết quả tìm kiếm
- **MainWindow.xaml.cs**: Xử lý sự kiện click nút "+" để thêm vào playlist

## Các Lưu Ý Kỹ Thuật

### 1. Kết Nối Database
- Sử dụng ADO.NET với Microsoft.Data.Sqlite
- Kết nối được mở và đóng sau mỗi thao tác
- Sử dụng parameterized queries để tránh SQL injection

### 2. Code Đơn Giản
- Không sử dụng async/await phức tạp
- Không sử dụng LINQ phức tạp
- Sử dụng vòng lặp for/foreach đơn giản
- Sử dụng if/else rõ ràng

### 3. Xử Lý Lỗi
- Try-catch để bắt lỗi
- MessageBox để thông báo lỗi cho user
- Throw exception với message rõ ràng

## Kiểm Tra Chức Năng

### Test Case 1: Thêm Bài Hát
1. Tìm kiếm bài hát "Love Story"
2. Click nút "+" ở một bài hát
3. Kiểm tra thông báo "Đã thêm ... vào playlist!"
4. Mở playlist, kiểm tra bài hát có trong danh sách

### Test Case 2: Thêm Bài Hát Trùng
1. Thêm một bài hát vào playlist
2. Thử thêm lại bài hát đó
3. Kiểm tra thông báo "Bài hát này đã có trong playlist!"

### Test Case 3: Xem Playlist
1. Click "Your Library" hoặc "My Playlist"
2. Kiểm tra danh sách bài hát hiển thị đúng
3. Kiểm tra số lượng bài hát hiển thị đúng

### Test Case 4: Phát Bài Hát
1. Mở playlist
2. Click nút Play ở một bài hát
3. Kiểm tra bài hát được phát
4. Kiểm tra thông tin hiển thị đúng

### Test Case 5: Xóa Bài Hát
1. Mở playlist
2. Click nút Delete ở một bài hát
3. Click Yes trong hộp thoại xác nhận
4. Kiểm tra bài hát bị xóa khỏi danh sách

### Test Case 6: Dữ Liệu Persistent
1. Thêm một vài bài hát vào playlist
2. Đóng app
3. Mở lại app
4. Mở playlist
5. Kiểm tra các bài hát vẫn còn trong playlist

## Troubleshooting

### Lỗi: "Không thể thêm bài hát vào playlist"
- Kiểm tra quyền ghi file trong thư mục %LocalAppData%
- Kiểm tra file database không bị corrupt

### Playlist Trống Sau Khi Mở App
- Kiểm tra file database có tồn tại tại `%LocalAppData%\MusicApp\playlist.db`
- Kiểm tra file có dữ liệu không bị lỗi

### Không Phát Được Bài Hát Từ Playlist
- Kiểm tra kết nối internet
- Kiểm tra URL preview của bài hát còn hợp lệ không

## Mở Rộng Trong Tương Lai

1. **Multiple Playlists**: Cho phép tạo nhiều playlist khác nhau
2. **Reorder**: Cho phép sắp xếp lại thứ tự bài hát
3. **Export/Import**: Xuất và nhập playlist dưới dạng file
4. **Shuffle**: Phát ngẫu nhiên các bài hát trong playlist
5. **Repeat**: Lặp lại playlist
6. **Search in Playlist**: Tìm kiếm bài hát trong playlist

## Kết Luận

Chức năng Playlist đã được triển khai hoàn chỉnh với đầy đủ các tính năng:
- ✅ Thêm bài hát vào playlist
- ✅ Xem danh sách playlist
- ✅ Phát bài hát từ playlist
- ✅ Xóa bài hát khỏi playlist
- ✅ Lưu trữ dữ liệu vĩnh viễn trong database
- ✅ Tải playlist khi khởi động app

Code được viết đơn giản, dễ hiểu, phù hợp cho người mới học C# và WPF.

