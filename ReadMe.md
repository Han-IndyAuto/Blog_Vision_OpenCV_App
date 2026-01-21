# 📄 WPF OpenCV 라이브러리를 이용한 비전프로그램 개발
---


## 📅 2026년 01월 21일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #33 적용. 
- Image Pyramid 연산 구현. 
- Gaussian Pyramid, Laplacian Pyramid 처리 가능하도록 추가. 
- PyramidUp, PyramidDown 함수 사용.
- Laplacian Pyramid 의 경우, 원본 이미지에서 cv2.pyUp() 함수를 통해 확대한 영상을 빼면 원본과 확대한 영상의 차이가 되며, 확대 영상에 더하면 원본 복원이 가능.
- UI의 상태바에 이미지 로딩 시 파일명과 해상도를 표시 하도록 수정하였으며, 이미지 처리 후에도 동일하게 표시되도록 수정.
- 체크 박스에 의해 원본 영상으로 돌아 갈때에도 상태바에 파일명과 해상도가 표시되도록 수정.

---


## 📅 2026년 01월 15일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #32 적용. 
- 영상 필터의 Morphology 연산 구현. 
- Morphology 에서 Erosion, dilation, Opening, Closing, Gradient, Top-Hat, Black-Hat 처리 가능하도록 추가.


---


## 📅 2026년 01월 09일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #31 적용. 
- 영상 필터의 Edged Detection Filter 에 7가지 항목에 대해 구현. 
- BasicDifferential, Roberts, Prewitt, Sobel, Scharr, Laplacian, Canny Edge 


---


## 📅 2026년 01월 07일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #30 적용. 
- 영상 필터링 에서 Average Blur, Box Filter, Gaussian Blur, Median Blur, Bilateral Filter 에 대해 OpenCV 함수 사용하여 구현.
- AutoFilterParameters 클래스 추가하여 필터 파라미터 관리.

---


## 📅 2026년 01월 06일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #29 적용. 
- 영상 필터링 에서 Convolution 과 kernel 행렬을 이용한 필터링 알고리즘(Blur, Sharpen, Edge) 추가.

---



## 📅 2026년 01월 02일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #28 적용. 
- Camera Calibration 알고리즘 추가. (Chessboard 패턴 인식 및 카메라 보정 구현)

---




## 📅 2025년 12월 29일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #27 적용. 
- Lens Distortion 알고리즘 추가. (볼록왜곡, 오목왜곡 구현)
- Manual Calculation 방식 으로 구현.
- Dubug 모드에서 조건부 컴파일 조건 설정: ManualCalculate 설정.

---


## 📅 2025년 12월 28일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #26 적용. 
- Lens Distortion 알고리즘 추가. (볼록왜곡, 오목왜곡 구현)
- OpenCV Polar 함수를 사용하여 구현.

---


## 📅 2025년 12월 27일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #25 적용. 
- Lens Distortion 알고리즘 추가. (Remap 함수 사용)

---


## 📅 2025년 12월 26일 (Second Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #24 적용. 
- Perspective Transform 알고리즘 추가. (getPerspectiveTransform 사용)
- 4점 지정하여 원근 변환 구현 (warpPerspective 구현)

---



## 📅 2025년 12월 26일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #23 적용. 
- Affine Transform 알고리즘 추가 (getAffineTransform 사용)
- 3점 지정하여 기하학적 변환 구현 (warpAffine 구현)

---

## 📅 2025년 12월 24일 (Second Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #22 적용. 
- Geometric Transform 알고리즘 추가 (warpAffine 구현)

---


## 📅 2025년 12월 24일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #21 적용. 
- Histogram 알고리즘 선택 후 실행 시 발생하는 버그 수정.
(이미지 로딩 후 히스토그램 선택하여 실행할때, ImgView 영역에 히스토그램 그래프가 그려지던 버그 수정)

---


## 📅 2025년 12월 22일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #17 ~ #20 적용. 
- Normalize 알고리즘 추가 (Normalize 윈도우 생성하여 그래프를 출력하도록 구현).
- Equalize 알고리즘 추가 (Equalize 윈도우 생성하여 그래프를 출력하도록 구현).
- CLAHE 알고리즘 추가 (CLAHE 윈도우 생성하여 그래프를 출력하도록 구현).

---

## 📅 2025년 12월 18일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #17 적용. 
- Histogram 알고리즘 추가 (Histogram 윈도우 생성하여 그래프를 출력하도록 구현).
- HistogramWindow.xaml 파일 추가.

---

## 📅 2025년 12월 17일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #14, #15, #16 적용. 
- Otsu 이진화 메뉴 추가.
- Adaptive Threshold 이진화 메뉴 추가.

---

## 📅 2025년 12월 10일 (First Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #13 적용. 
- Line, Circle, Rectangle 그리기 메뉴 추가.
- Line 을 그린 다음 끝점에 직선의 거리를 픽셀 값으로 표기하도록 코드 추가.

- WPF 기반의 OpenCV 영상처리 프로젝트 #13-1 적용. 
- 이미지 위에 ROI 사각형이 그려지고, 확대/축소하였을 때 ROI 사각형의 비율이 안맞는 문제 수정.

---

## 📅 2025년 12월 09일 (Second Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #12 적용.

---

## 📅 2025년 12월 09일 (Frist Commit)
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #11 적용.

---

## 📅 2025년 12월 05일
**작성자:** indy  
**검토자:** indy

### 1. 개요
- Blog 게시물 작성 코드: WPF와 OpenCV를 활용한 비전 프로그램 개발 방법 소개

### 2. 진행 내용
- WPF 기반의 OpenCV 영상처리 프로젝트 #9, #10 코드 적용.

---



