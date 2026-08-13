import { Routes, Route } from 'react-router-dom'
import MainPage from './app/MainPage'
import CartePage from './app/CartePage'


function App() {
  return (
    <div className="App">

      <Routes>
        <Route path="/" element={<MainPage />} />
        <Route path="/carte" element={<CartePage />} />
      </Routes>
    </div>
  );
}

export default App;