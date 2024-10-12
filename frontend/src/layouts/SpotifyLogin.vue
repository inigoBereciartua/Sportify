<template>
  <div>
    <h1>Login with Spotify</h1>
    <button @click="loginWithSpotify">Login</button>
  </div>
</template>

<script>
export default {
  name: 'SpotifyLogin',
  methods: {
    loginWithSpotify() {
      window.location.href = 'http://localhost:5000/login';
    }
  }
};
</script>
