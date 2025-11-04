# Track Detail View - Music App (Phần 5: Hiển thị thông tin bài hát - Kiên)

## 📝 Mô tả

Đây là phần **Track Detail View** cho ứng dụng nghe nhạc WPF - component hiển thị thông tin chi tiết của bài hát với UI hiện đại và tính năng nâng cao.

## ✨ Tính năng nổi bật

### 1. **Color-Adaptive UI (Giao diện thích ứng màu sắc)**
- Tự động extract dominant color từ album artwork
- Background gradient tự động thay đổi theo màu album
- Smooth color transitions với animations

### 2. **Glassmorphism Design**
- Hiệu ứng kính mờ (glass effect) hiện đại
- Semi-transparent cards với blur effects
- Depth và layers tạo cảm giác 3D

### 3. **Rich Information Display**
- **Thông tin cơ bản:**
  - Tên bài hát & nghệ sĩ
  - Album artwork với drop shadow
  - Duration, Popularity, Release Date
  - Album name

- **Audio Features (Độc đáo):**
  - Energy level (năng lượng)
  - Danceability (khả năng nhảy)
  - Valence/Mood (tâm trạng)
  - Acousticness (âm thanh acoustic)
  - Hiển thị dạng animated progress bars

- **Genre Tags:**
  - Thể loại nhạc với chip-based layout
  - Tự động capitalize genre names

### 4. **Smooth Animations**
- Fade-in khi load trang
- Slide-up entrance animation
- Animated progress bars cho audio features
- Color transitions cho background

### 5. **Interactive UI**
- Action buttons: Play, Favorite, Share
- Responsive hover effects
- Material Design components

## 🛠️ Tech Stack

| Công nghệ | Mục đích |
|-----------|----------|
| **WPF (.NET 9)** | UI Framework |
| **Material Design Themes** | Modern UI components |
| **Spotify Web API** | Lấy dữ liệu nhạc |
| **System.Drawing.Common** | Color extraction từ images |
| **Custom ColorExtractor** | Dominant color algorithm |

## 📁 Cấu trúc code

```
Group1.MusicApp/
├── Models/
│   ├── Track.cs              # Track data model
│   ├── Artist.cs             # Artist model
│   └── AudioFeatures.cs      # Audio features data
├── Views/
│   ├── TrackDetailView.xaml  # UI với Glassmorphism
│   └── TrackDetailView.xaml.cs # View logic
├── ViewModels/
│   └── TrackDetailViewModel.cs # Data fetching & transformation
└── Utilities/
    └── ColorExtractor.cs     # Color extraction algorithm
```

## 🎨 UI Features Chi tiết

### 1. Header Section
- Album cover 250x250px với rounded corners
- Track title (36px, Bold, White)
- Artist name (24px)
- Quick stats grid (Duration, Popularity, Date, Album)

### 2. Genre Section
- Wrap panel layout
- Chip-style tags
- Glassmorphism background

### 3. Audio Features Section
- 4 progress bars với labels
- Animated value changes
- Percentage display
- Visual representation của audio characteristics

### 4. Adaptive Background
- Linear gradient từ 2 màu extracted
- Smooth transitions (1s duration)
- Overlay với opacity để tăng độ contrast

## 🚀 Cách sử dụng

### Build & Run:
```bash
cd D:\Project\Music-WPF
dotnet build
dotnet run --project Group1.MusicApp
```

### Trong code:
```csharp
// 1. Khởi tạo ViewModel
var musicApi = new MusicAPI(clientId, clientSecret);
var viewModel = new TrackDetailViewModel(musicApi);

// 2. Fetch track details
var track = await viewModel.GetTrackDetailsAsync(trackId);

// 3. Load vào view
TrackDetailView.LoadTrack(track);
```

## 🎯 Điểm nổi bật so với yêu cầu

| Yêu cầu cơ bản | Implementation |
|----------------|----------------|
| Hiển thị ảnh, tên, ca sĩ | ✅ + Album art với shadows |
| Thông tin chi tiết | ✅ + Popularity, Duration, Release Date |
| **BONUS:** | |
| Audio Features | ✅ Energy, Danceability, Valence, Acousticness |
| Color Extraction | ✅ Dominant color từ album artwork |
| Adaptive UI | ✅ Background tự động đổi màu |
| Animations | ✅ Fade-in, Slide-up, Progress animations |
| Genre Tags | ✅ Chip-based layout với auto-capitalization |
| Glassmorphism | ✅ Modern glass effect design |

## 📊 API Endpoints được sử dụng

```
1. GET /tracks/{id}           - Track info
2. GET /audio-features/{id}   - Audio characteristics
3. GET /artists/{id}          - Artist info & genres
4. GET /albums/{id}           - Album details
```

## 🎥 Demo Flow

1. User nhập tên bài hát vào search box
2. Click vào track từ search results
3. TrackDetailView xuất hiện với animation
4. Background tự động đổi màu theo album art
5. Audio features animate lên với smooth transitions
6. User có thể thấy tất cả thông tin chi tiết

## 💡 Ghi chú kỹ thuật

### Color Extraction Algorithm:
- Sử dụng simplified "Color Thief" algorithm
- Sample pixels từ image với step size = 10
- Filter out very dark/light colors
- Quantize colors để reduce variations
- Find most common color cluster

### Performance:
- Async/await cho tất cả API calls
- Image caching với BitmapCacheOption.OnLoad
- Lazy loading của audio features

### Error Handling:
- Try-catch blocks cho API calls
- Fallback colors nếu extraction fails
- Graceful degradation nếu audio features không có

## 🔥 Điểm ấn tượng

1. **Color-Adaptive UI** - Hiếm có app WPF làm được
2. **Audio Features Visualization** - Unique feature
3. **Glassmorphism** - Trend design hiện đại
4. **Smooth Animations** - Professional UX
5. **Clean Architecture** - MVVM pattern

## 📝 License & Credits

- Spotify Web API - music data
- Material Design In XAML - UI components
- Developed by: Kiên (Track Detail View)
