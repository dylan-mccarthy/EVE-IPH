import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Navigation from './components/Navigation';
import AuthCallback from './pages/AuthCallback';
import CharacterSelect from './pages/CharacterSelect';
import CharacterProfile from './pages/CharacterProfile';
import Skills from './pages/Skills';

function App() {
  return (
    <BrowserRouter>
      <Navigation />
      <Routes>
        <Route path="/" element={<AuthCallback />} />
        <Route path="/auth/callback" element={<AuthCallback />} />
        <Route path="/characters" element={<CharacterSelect />} />
        <Route path="/characters/:characterId" element={<CharacterProfile />} />
        <Route path="/skills" element={<Skills />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
