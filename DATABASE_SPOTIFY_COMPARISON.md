# So sánh Database với Spotify API

## Bảng so sánh

| Spotify API (Track Model) | Database (PlaylistItems) | Mapping | Ghi chú |
|--------------------------|--------------------------|---------|---------|
| `Id` (string) | `TrackId` (TEXT NOT NULL UNIQUE) | ✓ | TrackId là unique để tránh trùng lặp |
| `Name` (string) | `TrackName` (TEXT NOT NULL) | ✓ | Tên bài hát |
| `ArtistName` (string) | `ArtistName` (TEXT NOT NULL) | ✓ | Tên nghệ sĩ (có thể null từ API, xử lý bằng `?? ""`) |
| `AlbumName` (string) | `AlbumName` (TEXT) | ✓ | Tên album (nullable) |
| `AlbumImageUrl` (string) | `AlbumImageUrl` (TEXT) | ✓ | URL hình ảnh album (nullable) |
| `DurationMs` (int) | `DurationMs` (INTEGER NOT NULL) | ✓ | Thời lượng bài hát (ms) |
| `PreviewUrl` (string) | `PreviewUrl` (TEXT) | ✓ | URL preview (nullable, không phải bài nào cũng có) |
| - | `AddedDate` (TEXT NOT NULL) | - | Ngày thêm vào playlist (thêm khi lưu) |
| - | `Id` (INTEGER PRIMARY KEY) | - | ID tự tăng trong database |

## Các trường không lưu (không cần cho playlist)

- `Popularity` - Độ phổ biến (không cần lưu trong playlist)
- `ReleaseDate` - Ngày phát hành (không cần lưu trong playlist)
- `Genres` - Thể loại (không cần lưu trong playlist)
- `IsExplicit` - Nội dung nhạy cảm (không cần lưu trong playlist)
- `AudioFeatures` - Đặc tính âm thanh (không cần lưu trong playlist)

## Kết luận

✅ **Database structure PHÙ HỢP với Spotify API**
- Tất cả các trường cần thiết đều được lưu
- Các trường nullable được xử lý đúng
- TrackId là UNIQUE để tránh trùng lặp bài hát

